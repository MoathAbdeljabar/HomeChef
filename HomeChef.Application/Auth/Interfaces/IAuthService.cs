
using HomeChef.Application.Auth.DTOs.Requests;
using HomeChef.Application.Auth.DTOs.Responses;
using HomeChef.Application.Shared;
using Identity.Domain.Enums;

namespace HomeChef.Application.Auth.Interfaces;
    public interface IAuthService
    {
    Task<ServiceResult<CreatedUserDto>> SignUpAsync(SignupRequestDto userInfo, EnRoles userRole = EnRoles.User);
    Task<ServiceResult<object>> SendPhoneVerificationCodeAsync(string phoneNumber);
    Task<ServiceResult<object>> VerifyPhoneNumberAsync(VerifyPhoneRequest request);
    Task<ServiceResult<AuthResponseDto>> LoginAsync(LoginRequestDto request);
    Task<ServiceResult<AuthResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto request);
    Task<ServiceResult<object>> LogoutAsync(string userId);
    Task<ServiceResult<object>> RequestPasswordResetAsync(string phoneNumber);
    Task<ServiceResult<object>> ResetPasswordAsync(ResetPasswordRequestDto request);

    }

