namespace UrlShortener.UserService.Application.Requests;

public class UpdateUserRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Role { get; set; }
    public string? Password { get; set; } // optional password update
}