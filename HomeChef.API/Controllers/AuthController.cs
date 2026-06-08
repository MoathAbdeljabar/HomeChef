using HomeChef.API.Helpers;
using HomeChef.Application.Auth.DTOs.Requests;
using HomeChef.Application.Auth.Interfaces;
using HomeChef.Application.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HomeChef.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("signup")]
    public async Task<IActionResult> Signup(SignupRequestDto request)
    {
        var result = await _authService.SignUpAsync(request);
        return result.ToActionResult();
    }

    [HttpPost("send-phone-verification")]
    public async Task<IActionResult> SendPhoneVerificationCode([FromBody] string phoneNumber)
    {
        var result = await _authService.SendPhoneVerificationCodeAsync(phoneNumber);
        return result.ToActionResult();
    }

    [HttpPost("verify-phone")]
    public async Task<IActionResult> VerifyPhoneNumber([FromBody] VerifyPhoneRequest request)
    {
        var result = await _authService.VerifyPhoneNumberAsync(request);
        return result.ToActionResult();
    }


    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var result = await _authService.LoginAsync(request);
        return result.ToActionResult();
    }

    
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request)
    {
        var result = await _authService.RefreshTokenAsync(request);
        return result.ToActionResult();
    }


    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var result = await _authService.LogoutAsync(userId);
        return result.ToActionResult();
    }


    //Request password reset
    [HttpPost("request-reset")]
    public async Task<IActionResult> RequestPasswordReset([FromBody] string phoneNumber)
    {
        var result = await _authService.RequestPasswordResetAsync(phoneNumber);
        return result.ToActionResult();
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request)
    {
        var result = await _authService.ResetPasswordAsync(request);
        return result.ToActionResult();
    }



    [HttpGet("hello")]
        [Authorize]
        public string SayHello()
        {
            return "Moath Abdeljabar";
        }
}