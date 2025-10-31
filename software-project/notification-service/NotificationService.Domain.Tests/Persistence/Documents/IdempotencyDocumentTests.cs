using FluentAssertions;
using NotificationService.Domain.Models;
using NotificationService.Domain.Persistence.Documents;

namespace NotificationService.Domain.Tests.Persistence.Documents;

public class IdempotencyDocumentTests
{
    [Fact]
    public void IdempotencyDocument_ShouldSetRequiredProperties()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow;
        var expiresAt = createdAt.AddHours(24);

        // Act
        var document = new IdempotencyDocument
        {
            Id = "idem-123",
            Key = "idem-123",
            CorrelationId = "corr-456",
            CreatedAt = createdAt,
            ExpiresAt = expiresAt,
            Status = IdempotencyStatus.Reserved
        };

        // Assert
        document.Id.Should().Be("idem-123");
        document.Key.Should().Be("idem-123");
        document.CorrelationId.Should().Be("corr-456");
        document.CreatedAt.Should().Be(createdAt);
        document.ExpiresAt.Should().Be(expiresAt);
        document.Status.Should().Be(IdempotencyStatus.Reserved);
    }

    [Fact]
    public void SetId_ShouldUpdateIdAndKey()
    {
        // Arrange
        var document = new IdempotencyDocument
        {
            CorrelationId = "corr-456",
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(24),
            Status = IdempotencyStatus.Reserved
        };

        // Act
        document.SetId("new-key");

        // Assert
        document.Id.Should().Be("new-key");
        document.Key.Should().Be("new-key");
    }

    [Theory]
    [InlineData(IdempotencyStatus.Reserved)]
    [InlineData(IdempotencyStatus.Queued)]
    [InlineData(IdempotencyStatus.Sent)]
    [InlineData(IdempotencyStatus.Failed)]
    public void IdempotencyDocument_ShouldSupportAllStatuses(IdempotencyStatus status)
    {
        // Arrange & Act
        var document = new IdempotencyDocument
        {
            Id = "idem-123",
            Key = "idem-123",
            CorrelationId = "corr-456",
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(24),
            Status = status
        };

        // Assert
        document.Status.Should().Be(status);
    }

    [Fact]
    public void IdempotencyDocument_ResponseJson_CanBeSet()
    {
        // Arrange
        var document = new IdempotencyDocument
        {
            Id = "idem-123",
            Key = "idem-123",
            CorrelationId = "corr-456",
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(24),
            Status = IdempotencyStatus.Sent
        };
        var jsonResponse = "{\"status\":\"sent\",\"messageId\":\"123\"}";

        // Act
        document.ResponseJson = jsonResponse;

        // Assert
        document.ResponseJson.Should().Be(jsonResponse);
    }

    [Fact]
    public void IdempotencyDocument_ResponseJson_ShouldBeNullByDefault()
    {
        // Arrange & Act
        var document = new IdempotencyDocument
        {
            Id = "idem-123",
            Key = "idem-123",
            CorrelationId = "corr-456",
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(24),
            Status = IdempotencyStatus.Reserved
        };

        // Assert
        document.ResponseJson.Should().BeNull();
    }

    [Fact]
    public void IdempotencyDocument_ExpiresAt_ShouldBeAfterCreatedAt()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow;
        var expiresAt = createdAt.AddDays(1);

        // Act
        var document = new IdempotencyDocument
        {
            Id = "idem-123",
            Key = "idem-123",
            CorrelationId = "corr-456",
            CreatedAt = createdAt,
            ExpiresAt = expiresAt,
            Status = IdempotencyStatus.Reserved
        };

        // Assert
        document.ExpiresAt.Should().BeAfter(document.CreatedAt);
    }
}
