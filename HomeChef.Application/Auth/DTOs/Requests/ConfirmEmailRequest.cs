namespace HomeChef.Application.Auth.DTOs.Requests;

public class ConfirmEmailRequest
{
    public string UserId { get; set; }
    public string Token { get; set; }
}
