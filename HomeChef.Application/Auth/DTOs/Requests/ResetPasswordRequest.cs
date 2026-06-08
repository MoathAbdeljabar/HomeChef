using System.ComponentModel.DataAnnotations;

namespace HomeChef.Application.Auth.DTOs.Requests;

public class ResetPasswordRequestDto 
{
    [Required]
    [Phone]
    public string PhoneNumber { get; set; }

    [Required]
    public string ResetToken { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string NewPassword { get; set; }
}