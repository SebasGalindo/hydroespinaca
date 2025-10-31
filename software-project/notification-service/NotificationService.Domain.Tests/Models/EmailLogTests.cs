using FluentAssertions;
using NotificationService.Domain.Models;

namespace NotificationService.Domain.Tests.Models;

public class EmailLogTests
{
    [Fact]
    public void EmailLog_ShouldInitializeWithRequiredProperties()
    {
        // Arrange & Act
        var emailLog = new EmailLog
        {
            Id = "log-123",
            CorrelationId = "corr-456",
            To = "test@example.com",
            Status = EmailDeliveryStatus.Queued
        };

        // Assert
        emailLog.Id.Should().Be("log-123");
        emailLog.CorrelationId.Should().Be("corr-456");
        emailLog.To.Should().Be("test@example.com");
        emailLog.Status.Should().Be(EmailDeliveryStatus.Queued);
    }

    [Fact]
    public void EmailLog_ShouldSetOptionalProperties()
    {
        // Arrange & Act
        var emailLog = new EmailLog
        {
            Id = "log-123",
            CorrelationId = "corr-456",
            To = "test@example.com",
            Subject = "Test Subject",
            Status = EmailDeliveryStatus.Sent,
            Provider = "SendGrid",
            ProviderMessageId = "msg-789",
            Error = null
        };

        // Assert
        emailLog.Subject.Should().Be("Test Subject");
        emailLog.Provider.Should().Be("SendGrid");
        emailLog.ProviderMessageId.Should().Be("msg-789");
        emailLog.Error.Should().BeNull();
    }

    [Fact]
    public void EmailLog_ShouldSetErrorOnFailure()
    {
        // Arrange & Act
        var emailLog = new EmailLog
        {
            Id = "log-123",
            CorrelationId = "corr-456",
            To = "test@example.com",
            Status = EmailDeliveryStatus.Failed,
            Error = "SMTP connection failed"
        };

        // Assert
        emailLog.Status.Should().Be(EmailDeliveryStatus.Failed);
        emailLog.Error.Should().Be("SMTP connection failed");
    }

    [Fact]
    public void SetId_ShouldUpdateIdProperty()
    {
        // Arrange
        var emailLog = new EmailLog
        {
            Id = "old-id",
            CorrelationId = "corr-456",
            To = "test@example.com",
            Status = EmailDeliveryStatus.Queued
        };

        // Act
        emailLog.SetId("new-id");

        // Assert
        emailLog.Id.Should().Be("new-id");
    }

    [Fact]
    public void CreatedAt_ShouldDefaultToCurrentTime()
    {
        // Arrange & Act
        var emailLog = new EmailLog
        {
            Id = "log-123",
            CorrelationId = "corr-456",
            To = "test@example.com",
            Status = EmailDeliveryStatus.Queued
        };

        // Assert
        emailLog.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void UpdatedAt_CanBeSet()
    {
        // Arrange
        var emailLog = new EmailLog
        {
            Id = "log-123",
            CorrelationId = "corr-456",
            To = "test@example.com",
            Status = EmailDeliveryStatus.Queued
        };
        var updateTime = DateTimeOffset.UtcNow.AddMinutes(5);

        // Act
        emailLog.UpdatedAt = updateTime;

        // Assert
        emailLog.UpdatedAt.Should().Be(updateTime);
    }

    [Theory]
    [InlineData(EmailDeliveryStatus.Queued)]
    [InlineData(EmailDeliveryStatus.Sent)]
    [InlineData(EmailDeliveryStatus.Failed)]
    public void Status_ShouldAcceptAllValidStatuses(EmailDeliveryStatus status)
    {
        // Arrange & Act
        var emailLog = new EmailLog
        {
            Id = "log-123",
            CorrelationId = "corr-456",
            To = "test@example.com",
            Status = status
        };

        // Assert
        emailLog.Status.Should().Be(status);
    }

    [Fact]
    public void Status_CanBeUpdated()
    {
        // Arrange
        var emailLog = new EmailLog
        {
            Id = "log-123",
            CorrelationId = "corr-456",
            To = "test@example.com",
            Status = EmailDeliveryStatus.Queued
        };

        // Act
        emailLog.Status = EmailDeliveryStatus.Sent;

        // Assert
        emailLog.Status.Should().Be(EmailDeliveryStatus.Sent);
    }
}
