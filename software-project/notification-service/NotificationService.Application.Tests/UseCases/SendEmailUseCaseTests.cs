using HydroEspinaca.Shared.DTOs.Notifications;
using Microsoft.Extensions.Logging;
using NotificationService.Application.UseCases;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces;
using NotificationService.Domain.Models;

namespace NotificationService.Application.Tests.UseCases;

public class SendEmailUseCaseTests
{
    private readonly Mock<IEmailQueue> _queueMock;
    private readonly Mock<ITemplateRenderer> _rendererMock;
    private readonly Mock<IIdempotencyStore> _idempotencyMock;
    private readonly Mock<IEmailLogRepository> _logRepoMock;
    private readonly Mock<ISanitizer> _sanitizerMock;
    private readonly Mock<INotificationGroupRepository> _groupRepoMock;
    private readonly Mock<ILogger<SendEmailUseCase>> _loggerMock;
    private readonly SendEmailUseCase _useCase;

    public SendEmailUseCaseTests()
    {
        _queueMock = new Mock<IEmailQueue>();
        _rendererMock = new Mock<ITemplateRenderer>();
        _idempotencyMock = new Mock<IIdempotencyStore>();
        _logRepoMock = new Mock<IEmailLogRepository>();
        _sanitizerMock = new Mock<ISanitizer>();
        _groupRepoMock = new Mock<INotificationGroupRepository>();
        _loggerMock = new Mock<ILogger<SendEmailUseCase>>();

        _useCase = new SendEmailUseCase(
            _queueMock.Object,
            _rendererMock.Object,
            _idempotencyMock.Object,
            _logRepoMock.Object,
            _sanitizerMock.Object,
            _groupRepoMock.Object,
            _loggerMock.Object
        );
    }

    #region Validation Tests

    [Fact]
    public async Task SendAsync_WithBothGroupAndTo_ShouldThrowException()
    {
        // Arrange
        var request = new SendEmailRequestDto
        {
            Group = "test-group",
            To = "test@example.com",
            Subject = "Test",
            HtmlBody = "<p>Test</p>"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _useCase.SendAsync(request, null, CancellationToken.None));
    }

    [Fact]
    public async Task SendAsync_WithNeitherGroupNorTo_ShouldThrowException()
    {
        // Arrange
        var request = new SendEmailRequestDto
        {
            Subject = "Test",
            HtmlBody = "<p>Test</p>"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _useCase.SendAsync(request, null, CancellationToken.None));
    }

    #endregion

    #region Direct Send Mode Tests

    [Fact]
    public async Task SendAsync_WithDirectMode_ShouldProcessSuccessfully()
    {
        // Arrange
        var request = new SendEmailRequestDto
        {
            To = "test@example.com",
            Subject = "Test Subject",
            HtmlBody = "<p>Test Body</p>"
        };

        var idempotencyKey = "test-key";
        var sanitizedBody = "<p>Sanitized</p>";
        var renderedHtml = "<html><body><p>Sanitized</p></body></html>";

        _idempotencyMock
            .Setup(x => x.TryReserveAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, new IdempotencyRecord
            {
                Key = "test-key",
                CorrelationId = "corr-123",
                CreatedAt = DateTimeOffset.UtcNow,
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(24),
                Status = IdempotencyStatus.Reserved
            }));

        _sanitizerMock
            .Setup(x => x.Sanitize(It.IsAny<string>()))
            .Returns(sanitizedBody);

        _rendererMock
            .Setup(x => x.RenderAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(renderedHtml);

        // Act
        var result = await _useCase.SendAsync(request, idempotencyKey, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be("queued");
        result.Id.Should().NotBeNullOrEmpty();

        _queueMock.Verify(x => x.EnqueueAsync(It.Is<EmailMessage>(m =>
            m.To.Length == 1 &&
            m.To[0] == "test@example.com" &&
            m.Subject == "Test Subject" &&
            m.HtmlBody == renderedHtml
        ), It.IsAny<CancellationToken>()), Times.Once);

        _logRepoMock.Verify(x => x.InsertAsync(It.Is<EmailLog>(log =>
            log.To == "test@example.com" &&
            log.Status == EmailDeliveryStatus.Queued
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_WithCcAndBcc_ShouldIncludeAllRecipients()
    {
        // Arrange
        var request = new SendEmailRequestDto
        {
            To = "to@example.com",
            Cc = new[] { "cc@example.com" },
            Bcc = new[] { "bcc@example.com" },
            Subject = "Test",
            HtmlBody = "<p>Test</p>"
        };

        SetupMocksForSuccessfulSend();

        // Act
        var result = await _useCase.SendAsync(request, "test-key", CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        _queueMock.Verify(x => x.EnqueueAsync(It.Is<EmailMessage>(m =>
            m.To.Length == 1 && m.To[0] == "to@example.com" &&
            m.Cc.Length == 1 && m.Cc[0] == "cc@example.com" &&
            m.Bcc.Length == 1 && m.Bcc[0] == "bcc@example.com"
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Group Send Mode Tests

    [Fact]
    public async Task SendAsync_WithGroupMode_ShouldResolveGroupRecipients()
    {
        // Arrange
        var request = new SendEmailRequestDto
        {
            Group = "test-group",
            Subject = "Test",
            HtmlBody = "<p>Test</p>"
        };

        var group = new NotificationGroup
        {
            GroupName = "test-group",
            Recipients = new List<GroupRecipient>
            {
                new() { Email = "user1@example.com", Type = RecipientType.TO, IsActive = true },
                new() { Email = "user2@example.com", Type = RecipientType.CC, IsActive = true }
            }
        };

        _groupRepoMock
            .Setup(x => x.GetByGroupNameAsync("test-group", It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);

        SetupMocksForSuccessfulSend();

        // Act
        var result = await _useCase.SendAsync(request, "test-key", CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        _queueMock.Verify(x => x.EnqueueAsync(It.Is<EmailMessage>(m =>
            m.To.Contains("user1@example.com") &&
            m.Cc.Contains("user2@example.com")
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_WithNonExistentGroup_ShouldThrowException()
    {
        // Arrange
        var request = new SendEmailRequestDto
        {
            Group = "non-existent-group",
            Subject = "Test",
            HtmlBody = "<p>Test</p>"
        };

        _groupRepoMock
            .Setup(x => x.GetByGroupNameAsync("non-existent-group", It.IsAny<CancellationToken>()))
            .ReturnsAsync((NotificationGroup?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _useCase.SendAsync(request, "test-key", CancellationToken.None));

        exception.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task SendAsync_WithGroupWithNoActiveRecipients_ShouldThrowException()
    {
        // Arrange
        var request = new SendEmailRequestDto
        {
            Group = "empty-group",
            Subject = "Test",
            HtmlBody = "<p>Test</p>"
        };

        var group = new NotificationGroup
        {
            GroupName = "empty-group",
            Recipients = new List<GroupRecipient>
            {
                new() { Email = "inactive@example.com", IsActive = false }
            }
        };

        _groupRepoMock
            .Setup(x => x.GetByGroupNameAsync("empty-group", It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _useCase.SendAsync(request, "test-key", CancellationToken.None));

        exception.Message.Should().Contain("no active recipients");
    }

    #endregion

    #region Idempotency Tests

    [Fact]
    public async Task SendAsync_WithIdempotentRequest_ShouldReturnCachedResult()
    {
        // Arrange
        var request = new SendEmailRequestDto
        {
            To = "test@example.com",
            Subject = "Test",
            HtmlBody = "<p>Test</p>"
        };

        var existingRecord = new IdempotencyRecord
        {
            Key = "duplicate-key",
            CorrelationId = "existing-corr-id",
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(24),
            Status = IdempotencyStatus.Sent
        };

        _idempotencyMock
            .Setup(x => x.TryReserveAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, existingRecord));

        // Act
        var result = await _useCase.SendAsync(request, "duplicate-key", CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be("existing-corr-id");
        result.Status.Should().Be("sent");

        _queueMock.Verify(x => x.EnqueueAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendAsync_WithoutIdempotencyKey_ShouldGenerateOne()
    {
        // Arrange
        var request = new SendEmailRequestDto
        {
            To = "test@example.com",
            Subject = "Test",
            HtmlBody = "<p>Test</p>"
        };

        SetupMocksForSuccessfulSend();

        // Act
        var result = await _useCase.SendAsync(request, null, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        _idempotencyMock.Verify(x => x.TryReserveAsync(
            It.Is<string>(key => !string.IsNullOrEmpty(key)),
            It.IsAny<string>(),
            It.IsAny<TimeSpan>(),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    #endregion

    #region Sanitization and Rendering Tests

    [Fact]
    public async Task SendAsync_ShouldSanitizeHtmlBody()
    {
        // Arrange
        var request = new SendEmailRequestDto
        {
            To = "test@example.com",
            Subject = "Test",
            HtmlBody = "<p>Potentially unsafe content</p>"
        };

        var sanitizedBody = "<p>Safe content</p>";
        _sanitizerMock
            .Setup(x => x.Sanitize("< p>Potentially unsafe content</p>"))
            .Returns(sanitizedBody);

        SetupMocksForSuccessfulSend();

        // Act
        await _useCase.SendAsync(request, "test-key", CancellationToken.None);

        // Assert
        _sanitizerMock.Verify(x => x.Sanitize(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_ShouldRenderTemplate()
    {
        // Arrange
        var request = new SendEmailRequestDto
        {
            To = "test@example.com",
            Subject = "Test",
            HtmlBody = "<p>Test</p>"
        };

        var renderedHtml = "<html><body><p>Test</p></body></html>";
        _rendererMock
            .Setup(x => x.RenderAsync("base", It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(renderedHtml);

        SetupMocksForSuccessfulSend();

        // Act
        await _useCase.SendAsync(request, "test-key", CancellationToken.None);

        // Assert
        _rendererMock.Verify(x => x.RenderAsync("base", It.IsAny<string>(), null, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Helper Methods

    private void SetupMocksForSuccessfulSend()
    {
        _idempotencyMock
            .Setup(x => x.TryReserveAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, new IdempotencyRecord
            {
                Key = "test-key",
                CorrelationId = "corr-123",
                CreatedAt = DateTimeOffset.UtcNow,
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(24),
                Status = IdempotencyStatus.Reserved
            }));

        _sanitizerMock
            .Setup(x => x.Sanitize(It.IsAny<string>()))
            .Returns((string input) => input);

        _rendererMock
            .Setup(x => x.RenderAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string template, string body, object model, CancellationToken ct) => $"<html>{body}</html>");

        _queueMock
            .Setup(x => x.EnqueueAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _logRepoMock
            .Setup(x => x.InsertAsync(It.IsAny<EmailLog>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _idempotencyMock
            .Setup(x => x.UpdateAsync(It.IsAny<string>(), It.IsAny<Action<IdempotencyRecord>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    #endregion
}
