using System.ComponentModel.DataAnnotations;

namespace HomeChef.Application.Auth.DTOs.Requests;

public class RefreshTokenRequestDto
{
    [Required]
    public string AccessToken { get; set; } = string.Empty;
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}


