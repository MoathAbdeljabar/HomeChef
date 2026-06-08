using System.ComponentModel.DataAnnotations;

namespace HomeChef.Application.Auth.DTOs.Requests;

public class SignupRequestDto
{
    [Required(ErrorMessage = "First Name is required")]
    [Length(3, 10)]
    public string FirstName { get; set; }

    [Required(ErrorMessage = "Last Name is required")]
    public string LastName { get; set; }

    [Required(ErrorMessage = "Birth Date is required")]
    public DateOnly BirthDate { get; set; }

    //[Required(ErrorMessage = "Email is required")]
    //[EmailAddress(ErrorMessage = "Invalid Email Address")]
    //public string Email { get; set; }

    [Required(ErrorMessage = "Password is required")]
    public string Password { get; set; }

    [Required(ErrorMessage = "Phone number is required")]
    [Phone(ErrorMessage = "Invalid phone number")]
    [RegularExpression(@"^\+?[1-9]\d{1,14}$", ErrorMessage = "Invalid phone number format")]
    public string PhoneNumber { get; set; }

    [Required(ErrorMessage = "Gender is required")]
    [EnumDataType(typeof(EnGender), ErrorMessage = "Invalid gender value")]
    public EnGender Gender { get; set; }
}