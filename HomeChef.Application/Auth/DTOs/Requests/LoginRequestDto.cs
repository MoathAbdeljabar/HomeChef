using System.ComponentModel.DataAnnotations;

namespace HomeChef.Application.Auth.DTOs.Requests;
    public class LoginRequestDto
    {
    [Required]
    [Phone]
    public string PhoneNumber { get; set; }

    [Required]
    public string Password { get; set; }


}


