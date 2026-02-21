namespace NotificationService.Api.Contracts.Requests;

public class SendMultiChannelRequest
{
    public required string UserId { get; set; }
    public required string TemplateKey { get; set; }
    public required string Title { get; set; }
    public required string Body { get; set; }
    public Dictionary<string, string>? Data { get; set; }
}
