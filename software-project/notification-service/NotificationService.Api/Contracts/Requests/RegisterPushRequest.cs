namespace NotificationService.Api.Contracts.Requests;

public class RegisterPushRequest
{
    public required string UserId { get; set; }
    public required string Platform { get; set; }
    public required string Token { get; set; }
    public string? DeviceName { get; set; }
}
