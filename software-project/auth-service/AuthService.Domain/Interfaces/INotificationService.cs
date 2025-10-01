namespace AuthService.Domain.Interfaces;

/// <summary>
/// Interface for sending notifications via notification-service
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Sends an email notification
    /// </summary>
    /// <param name="to">Recipient email address</param>
    /// <param name="subject">Email subject</param>
    /// <param name="htmlBody">HTML email body</param>
    /// <returns>True if email was sent successfully</returns>
    Task<bool> SendEmailAsync(string to, string subject, string htmlBody);
}