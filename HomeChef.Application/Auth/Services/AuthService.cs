

using HomeChef.Application.Auth.DTOs.Requests;
using HomeChef.Application.Auth.DTOs.Responses;
using HomeChef.Application.Auth.Interfaces;
using HomeChef.Application.Shared;
using HomeChef.Data;
using HomeChef.Data.Models;
using Identity.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace HomeChef.Application.Auth.Services;
public partial class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    public AuthService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration
        ) {
        _context = context;
        _userManager = userManager;
        _configuration = configuration;
    }
    public async Task<ServiceResult<CreatedUserDto>> SignUpAsync(SignupRequestDto request, EnRoles userRole = EnRoles.User)
    {
        if(await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber) != null)
            {
            return ServiceResult<CreatedUserDto>.Failure(
           BusinessErrorType.PhoneNumberAlreadyExists,
           "This phone number is already associated with an existing account. " +
           "Please use a different phone number or sign in to your existing account.");
        }

        if (!IsOver18(request.BirthDate))
        {
            return ServiceResult<CreatedUserDto>.Failure(BusinessErrorType.CanNotCreateUser, "You must be at least 18 years old");
        }

        var user = new ApplicationUser
        {
            UserName = request.PhoneNumber,
            PhoneNumber = request.PhoneNumber,
            FirstName = request.FirstName,
            LastName = request.LastName,
            BirthDate = request.BirthDate,
            Gender = request.Gender,

        };


        await using var transaction = await _context.Database.BeginTransactionAsync();

        IdentityResult result = await _userManager.CreateAsync(user, request.Password);

        if (result.Succeeded)
        {

            // Assign default role 
            var addToRoleResult = await _userManager.AddToRoleAsync(user, userRole.ToString());

            if (!addToRoleResult.Succeeded)
            {

                await transaction.RollbackAsync();

                var errors = addToRoleResult.Errors.Select(e => e.Description);
                ;
                return ServiceResult<CreatedUserDto>.Failure(
                    errorType: BusinessErrorType.CanNotCreateUser,
                    message: "Role Error " + string.Join(", ", errors)
                );
            }
            await transaction.CommitAsync();

            return ServiceResult<CreatedUserDto>.Success(
                data: new CreatedUserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    Username = user.UserName,

                },
              message: "User registered successfully. Please confirm your phone number."
            );
        }
        else
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            await transaction.RollbackAsync();
            return ServiceResult<CreatedUserDto>.Failure(
                errorType: BusinessErrorType.CanNotCreateUser,
                message: "User creation failed: " + string.Join(", ", errors)
            );
        }
    }

    public async Task<ServiceResult<object>> SendPhoneVerificationCodeAsync(string phoneNumber)
    {
        var user = await _userManager.FindByNameAsync(phoneNumber);

        if (user != null && !user.PhoneNumberConfirmed)
        {
            var token = await _userManager.GenerateChangePhoneNumberTokenAsync(
                user, user.PhoneNumber);

            Console.WriteLine(token);
            //await _smsService.SendSmsAsync(user.PhoneNumber,
            //    $"Your verification code is: {token}");

            // Log: Code sent successfully
        }
        else
        {
            // Log: User not found OR phone already confirmed
        }

        return ServiceResult<object>.Success(null,
            "If the phone number is registered, a verification code will be sent.");

    }

    public async Task<ServiceResult<object>> VerifyPhoneNumberAsync(VerifyPhoneRequest request)
    {
        var user = await _userManager.FindByNameAsync(request.PhoneNumber);
        if (user != null && !user.PhoneNumberConfirmed)
        {
            var isTokenValid = await _userManager.VerifyChangePhoneNumberTokenAsync(
                user, request.VerificationCode , request.PhoneNumber);
            if (isTokenValid)
            {
                user.PhoneNumberConfirmed = true;
                await _userManager.UpdateAsync(user);
                return ServiceResult<object>.Success(true, "Phone number verified successfully");
            }

            //Log
        }
        else
        {
            // Log: User not found OR phone already confirmed

        }
        return ServiceResult<object>.Success(BusinessErrorType.ValidationFailed, "Verification failed. Please try again.");

    }

    public async Task<ServiceResult<AuthResponseDto>> LoginAsync(LoginRequestDto request){
        var user = await _userManager.FindByNameAsync(request.PhoneNumber);
        if(user is null)
        {
            return ServiceResult<AuthResponseDto>.Failure(BusinessErrorType.InvalidCredentials, "");
        }



        if (!await _userManager.CheckPasswordAsync(user, request.Password))
        {
            // This will increment the failed login count
            await _userManager.AccessFailedAsync(user);
            return ServiceResult<AuthResponseDto>.Failure(BusinessErrorType.InvalidCredentials, "Invalid Credentials");

        }

        if (!user.PhoneNumberConfirmed)
        {
            return ServiceResult<AuthResponseDto>.Failure(BusinessErrorType.PhoneNotConfirmed, "Please confirm your phone number");
        }
        var loginTokens = await GenerateLoginTokens(user);
        return ServiceResult<AuthResponseDto>.Success(loginTokens, "Login successful");
    }

    private async Task<AuthResponseDto> GenerateLoginTokens(ApplicationUser user)
    {
        // Reset failed count on successful password verification
        await _userManager.ResetAccessFailedCountAsync(user);

        // Generate tokens
        var accessToken = await _GenerateJwtTokenAsync(user);
        var refreshToken = GenerateRefreshToken();

        // Store refresh token in database
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7); // Refresh token valid for 7 days
        await _userManager.UpdateAsync(user);


        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpiry = DateTime.UtcNow.AddMinutes(GetAccessTokenExpiryMinutes())
        };
    }



    //--------------------------------------
    //Refersh Token
    //--------------------------------------

    public async Task<ServiceResult<AuthResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto request)
    {
        try
        {
            // Get user from expired token
            var principal = GetPrincipalFromExpiredToken(request.AccessToken);
            //Principal = The authenticated user's identity + claims (like a user passport)
            //Principal = User's identity extracted from the expired token
            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            //FindFirst(ClaimTypes.NameIdentifier) - Gets the User ID from the token claims


            if (string.IsNullOrEmpty(userId))
            {
                return ServiceResult<AuthResponseDto>.Failure(BusinessErrorType.InvalidToken, "Invalid token");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return ServiceResult<AuthResponseDto>.Failure(BusinessErrorType.InvalidToken, "User not found");
            }

            // Validate refresh token
            if (user.RefreshToken != request.RefreshToken || user.RefreshTokenExpiry <= DateTime.UtcNow)
            {
                return ServiceResult<AuthResponseDto>.Failure(BusinessErrorType.InvalidToken, "Invalid or expired refresh token");
            }

            // Generate new access token
            var newAccessToken = await _GenerateJwtTokenAsync(user);

            // Generate new refresh token (rotation) but keep original expiry
            var newRefreshToken = GenerateRefreshToken();

            // Update refresh token in database - KEEP ORIGINAL EXPIRY
            user.RefreshToken = newRefreshToken;
            // RefreshTokenExpiry stays the same (don't extend it)
            await _userManager.UpdateAsync(user);

            var response = new AuthResponseDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                AccessTokenExpiry = DateTime.UtcNow.AddMinutes(GetAccessTokenExpiryMinutes())
            };

            return ServiceResult<AuthResponseDto>.Success(response, "Token refreshed successfully");
        }
        catch (SecurityTokenException)
        {
            return ServiceResult<AuthResponseDto>.Failure(BusinessErrorType.InvalidToken, "Invalid token");
        }
    }



    public async Task<ServiceResult<object>> LogoutAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user != null)
        {
            // Revoke refresh token - prevents getting new access tokens
            user.RefreshToken = null;
            user.RefreshTokenExpiry = DateTime.MinValue;

            // ===== ADDED: Update security stamp =====
            // This invalidates ALL existing JWT tokens for this user immediately
            // The old tokens will fail validation because they contain the old stamp
            // This is more secure than just removing the refresh token
            await _userManager.UpdateSecurityStampAsync(user);

            await _userManager.UpdateAsync(user);
        }
        else
        {
            return ServiceResult<object>.Failure(BusinessErrorType.InvalidToken, "Can not logout");
        }

        return ServiceResult<object>.Success(null, "Logged out successfully");
    }



    public async Task<ServiceResult<object>>  RequestPasswordResetAsync(string phoneNumber)
    {
        var user = await _userManager.Users
          .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);

        if (user != null)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            //await smsService.SendSmsAsync(phoneNumber, $"Your password reset code is: {token}");
        }
        return ServiceResult<object>.Success(null, "If the phone number exists, a reset code has been sent.");
    }

    public async Task<ServiceResult<object>> ResetPasswordAsync(ResetPasswordRequestDto request)
    {
        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber);

        if (user == null)
        {
            return ServiceResult<object>.Failure(BusinessErrorType.ValidationFailed, "Invalid phone number or token");
        }

        var result = await _userManager.ResetPasswordAsync(
            user,
            request.ResetToken,
            request.NewPassword
        );



        if (result.Succeeded)
        {
            // Optional: Expire any existing sessions
            await _userManager.UpdateSecurityStampAsync(user);

            return ServiceResult<object>.Success(null, "Password has been reset successfully.");
        }else
        {
            return ServiceResult<object>.Failure(BusinessErrorType.ValidationFailed, "Invalid phone number or token");


            //return BadRequest(new
            //{
            //    message = "Password reset failed.",
            //    errors = result.Errors
            //});
        }

    }


}

