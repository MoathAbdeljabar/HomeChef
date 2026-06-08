
using System.ComponentModel.DataAnnotations;

namespace HomeChef.Application.Auth.DTOs.Requests;
public class VerifyPhoneRequest
{
    [Required]
    [Phone]
    public string PhoneNumber { get; set; }

    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string VerificationCode { get; set; }
}