using FluentAssertions;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Persistence.Documents;

namespace NotificationService.Domain.Tests.Persistence.Documents;

public class EmailLogDocumentTests
{
    [Fact]
    public void EmailLogDocument_ShouldSetRequiredProperties()
    {
        // Arrange & Act
        var document = new EmailLogDocument
        {
            Id = "log-123",
            CorrelationId = "corr-456",
            To = "recipient@example.com",
            Status = EmailDeliveryStatus.Queued,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Assert
        document.Id.Should().Be("log-123");
        document.CorrelationId.Should().Be("corr-456");
        document.To.Should().Be("recipient@example.com");
        document.Status.Should().Be(EmailDeliveryStatus.Queued);
    }

    [Fact]
    public void EmailLogDocument_ShouldSetOptionalProperties()
    {
        // Arrange & Act
        var document = new EmailLogDocument
        {
            Id = "log-123",
            CorrelationId = "corr-456",
            To = "recipient@example.com",
            Subject = "Test Subject",
            Status = EmailDeliveryStatus.Sent,
            Provider = "SendGrid",
            ProviderMessageId = "msg-789",
            Error = null,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        // Assert
        document.Subject.Should().Be("Test Subject");
        document.Provider.Should().Be("SendGrid");
        document.ProviderMessageId.Should().Be("msg-789");
        document.Error.Should().BeNull();
        document.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void SetId_ShouldUpdateIdProperty()
    {
        // Arrange
        var document = new EmailLogDocument
        {
            CorrelationId = "corr-456",
            To = "recipient@example.com",
            Status = EmailDeliveryStatus.Queued,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Act
        document.SetId("new-log-id");

        // Assert
        document.Id.Should().Be("new-log-id");
    }

    [Theory]
    [InlineData(EmailDeliveryStatus.Queued)]
    [InlineData(EmailDeliveryStatus.Sent)]
    [InlineData(EmailDeliveryStatus.Failed)]
    public void EmailLogDocument_ShouldSupportAllStatuses(EmailDeliveryStatus status)
    {
        // Arrange & Act
        var document = new EmailLogDocument
        {
            Id = "log-123",
            CorrelationId = "corr-456",
            To = "recipient@example.com",
            Status = status,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Assert
        document.Status.Should().Be(status);
    }

    [Fact]
    public void EmailLogDocument_WithError_ShouldStoreErrorMessage()
    {
        // Arrange & Act
        var document = new EmailLogDocument
        {
            Id = "log-123",
            CorrelationId = "corr-456",
            To = "recipient@example.com",
            Status = EmailDeliveryStatus.Failed,
            Error = "SMTP connection failed",
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Assert
        document.Status.Should().Be(EmailDeliveryStatus.Failed);
        document.Error.Should().Be("SMTP connection failed");
    }
}
