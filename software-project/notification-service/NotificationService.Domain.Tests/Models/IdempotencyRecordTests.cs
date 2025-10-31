using FluentAssertions;
using NotificationService.Domain.Models;

namespace NotificationService.Domain.Tests.Models;

public class IdempotencyRecordTests
{
    [Fact]
    public void IdempotencyRecord_ShouldInitializeWithRequiredProperties()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow;
        var expiresAt = createdAt.AddHours(24);

        // Act
        var record = new IdempotencyRecord
        {
            Key = "idempotency-key-123",
            CorrelationId = "corr-456",
            CreatedAt = createdAt,
            ExpiresAt = expiresAt,
            Status = IdempotencyStatus.Reserved
        };

        // Assert
        record.Key.Should().Be("idempotency-key-123");
        record.CorrelationId.Should().Be("corr-456");
        record.CreatedAt.Should().Be(createdAt);
        record.ExpiresAt.Should().Be(expiresAt);
        record.Status.Should().Be(IdempotencyStatus.Reserved);
    }

    [Fact]
    public void Id_ShouldReturnKey()
    {
        // Arrange
        var record = new IdempotencyRecord
        {
            Key = "idempotency-key-123",
            CorrelationId = "corr-456",
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(24),
            Status = IdempotencyStatus.Reserved
        };

        // Act & Assert
        record.Id.Should().Be("idempotency-key-123");
        record.Id.Should().Be(record.Key);
    }

    [Fact]
    public void SetId_ShouldDoNothing()
    {
        // Arrange
        var record = new IdempotencyRecord
        {
            Key = "original-key",
            CorrelationId = "corr-456",
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(24),
            Status = IdempotencyStatus.Reserved
        };

        // Act
        record.SetId("new-key");

        // Assert - Id should still be the original key
        record.Id.Should().Be("original-key");
        record.Key.Should().Be("original-key");
    }

    [Theory]
    [InlineData(IdempotencyStatus.Reserved)]
    [InlineData(IdempotencyStatus.Queued)]
    [InlineData(IdempotencyStatus.Sent)]
    [InlineData(IdempotencyStatus.Failed)]
    public void Status_ShouldAcceptAllValidStatuses(IdempotencyStatus status)
    {
        // Arrange & Act
        var record = new IdempotencyRecord
        {
            Key = "idempotency-key-123",
            CorrelationId = "corr-456",
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(24),
            Status = status
        };

        // Assert
        record.Status.Should().Be(status);
    }

    [Fact]
    public void Status_CanBeUpdated()
    {
        // Arrange
        var record = new IdempotencyRecord
        {
            Key = "idempotency-key-123",
            CorrelationId = "corr-456",
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(24),
            Status = IdempotencyStatus.Reserved
        };

        // Act
        record.Status = IdempotencyStatus.Queued;

        // Assert
        record.Status.Should().Be(IdempotencyStatus.Queued);
    }

    [Fact]
    public void ResponseJson_CanBeSet()
    {
        // Arrange
        var record = new IdempotencyRecord
        {
            Key = "idempotency-key-123",
            CorrelationId = "corr-456",
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(24),
            Status = IdempotencyStatus.Sent
        };
        var jsonResponse = "{\"messageId\":\"123\",\"status\":\"sent\"}";

        // Act
        record.ResponseJson = jsonResponse;

        // Assert
        record.ResponseJson.Should().Be(jsonResponse);
    }

    [Fact]
    public void ResponseJson_ShouldBeNullByDefault()
    {
        // Arrange & Act
        var record = new IdempotencyRecord
        {
            Key = "idempotency-key-123",
            CorrelationId = "corr-456",
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(24),
            Status = IdempotencyStatus.Reserved
        };

        // Assert
        record.ResponseJson.Should().BeNull();
    }

    [Fact]
    public void ExpiresAt_ShouldBeAfterCreatedAt()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow;
        var expiresAt = createdAt.AddHours(24);

        // Act
        var record = new IdempotencyRecord
        {
            Key = "idempotency-key-123",
            CorrelationId = "corr-456",
            CreatedAt = createdAt,
            ExpiresAt = expiresAt,
            Status = IdempotencyStatus.Reserved
        };

        // Assert
        record.ExpiresAt.Should().BeAfter(record.CreatedAt);
    }
}
