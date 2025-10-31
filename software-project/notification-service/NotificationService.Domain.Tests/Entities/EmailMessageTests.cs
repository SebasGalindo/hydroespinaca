using FluentAssertions;
using NotificationService.Domain.Entities;

namespace NotificationService.Domain.Tests.Entities;

public class EmailMessageTests
{
    [Fact]
    public void EmailMessage_ShouldInitializeWithRequiredProperties()
    {
        // Arrange & Act
        var message = new EmailMessage
        {
            CorrelationId = "corr-123",
            IdempotencyKey = "idem-456",
            To = new[] { "recipient@example.com" },
            Subject = "Test Subject",
            HtmlBody = "<p>Test Body</p>",
            TemplateKey = "welcome-template"
        };

        // Assert
        message.CorrelationId.Should().Be("corr-123");
        message.IdempotencyKey.Should().Be("idem-456");
        message.To.Should().ContainSingle().And.Contain("recipient@example.com");
        message.Subject.Should().Be("Test Subject");
        message.HtmlBody.Should().Be("<p>Test Body</p>");
        message.TemplateKey.Should().Be("welcome-template");
    }

    [Fact]
    public void EmailMessage_ShouldSupportMultipleToRecipients()
    {
        // Arrange & Act
        var message = new EmailMessage
        {
            CorrelationId = "corr-123",
            IdempotencyKey = "idem-456",
            To = new[] { "recipient1@example.com", "recipient2@example.com" },
            Subject = "Test Subject",
            HtmlBody = "<p>Test Body</p>",
            TemplateKey = "welcome-template"
        };

        // Assert
        message.To.Should().HaveCount(2);
        message.To.Should().Contain(new[] { "recipient1@example.com", "recipient2@example.com" });
    }

    [Fact]
    public void EmailMessage_Cc_ShouldDefaultToEmptyArray()
    {
        // Arrange & Act
        var message = new EmailMessage
        {
            CorrelationId = "corr-123",
            IdempotencyKey = "idem-456",
            To = new[] { "recipient@example.com" },
            Subject = "Test Subject",
            HtmlBody = "<p>Test Body</p>",
            TemplateKey = "welcome-template"
        };

        // Assert
        message.Cc.Should().BeEmpty();
    }

    [Fact]
    public void EmailMessage_Bcc_ShouldDefaultToEmptyArray()
    {
        // Arrange & Act
        var message = new EmailMessage
        {
            CorrelationId = "corr-123",
            IdempotencyKey = "idem-456",
            To = new[] { "recipient@example.com" },
            Subject = "Test Subject",
            HtmlBody = "<p>Test Body</p>",
            TemplateKey = "welcome-template"
        };

        // Assert
        message.Bcc.Should().BeEmpty();
    }

    [Fact]
    public void EmailMessage_ShouldSupportCcAndBcc()
    {
        // Arrange & Act
        var message = new EmailMessage
        {
            CorrelationId = "corr-123",
            IdempotencyKey = "idem-456",
            To = new[] { "to@example.com" },
            Cc = new[] { "cc1@example.com", "cc2@example.com" },
            Bcc = new[] { "bcc@example.com" },
            Subject = "Test Subject",
            HtmlBody = "<p>Test Body</p>",
            TemplateKey = "welcome-template"
        };

        // Assert
        message.Cc.Should().HaveCount(2);
        message.Cc.Should().Contain(new[] { "cc1@example.com", "cc2@example.com" });
        message.Bcc.Should().ContainSingle().And.Contain("bcc@example.com");
    }

    [Fact]
    public void EmailMessage_Attachments_ShouldDefaultToEmptyList()
    {
        // Arrange & Act
        var message = new EmailMessage
        {
            CorrelationId = "corr-123",
            IdempotencyKey = "idem-456",
            To = new[] { "recipient@example.com" },
            Subject = "Test Subject",
            HtmlBody = "<p>Test Body</p>",
            TemplateKey = "welcome-template"
        };

        // Assert
        message.Attachments.Should().NotBeNull();
        message.Attachments.Should().BeEmpty();
    }

    [Fact]
    public void EmailMessage_ShouldSupportAttachments()
    {
        // Arrange & Act
        var attachment = new EmailAttachment
        {
            FileName = "document.pdf",
            ContentType = "application/pdf",
            ContentBase64 = "base64content=="
        };

        var message = new EmailMessage
        {
            CorrelationId = "corr-123",
            IdempotencyKey = "idem-456",
            To = new[] { "recipient@example.com" },
            Subject = "Test Subject",
            HtmlBody = "<p>Test Body</p>",
            TemplateKey = "welcome-template",
            Attachments = new List<EmailAttachment> { attachment }
        };

        // Assert
        message.Attachments.Should().ContainSingle();
        message.Attachments.First().FileName.Should().Be("document.pdf");
    }
}

public class EmailAttachmentTests
{
    [Fact]
    public void EmailAttachment_ShouldInitializeWithRequiredProperties()
    {
        // Arrange & Act
        var attachment = new EmailAttachment
        {
            FileName = "document.pdf",
            ContentType = "application/pdf",
            ContentBase64 = "base64encodedcontent=="
        };

        // Assert
        attachment.FileName.Should().Be("document.pdf");
        attachment.ContentType.Should().Be("application/pdf");
        attachment.ContentBase64.Should().Be("base64encodedcontent==");
    }

    [Fact]
    public void EmailAttachment_ShouldSupportDifferentFileTypes()
    {
        // Arrange & Act
        var pdfAttachment = new EmailAttachment
        {
            FileName = "report.pdf",
            ContentType = "application/pdf",
            ContentBase64 = "pdfcontent=="
        };

        var imageAttachment = new EmailAttachment
        {
            FileName = "image.png",
            ContentType = "image/png",
            ContentBase64 = "imagecontent=="
        };

        // Assert
        pdfAttachment.ContentType.Should().Be("application/pdf");
        imageAttachment.ContentType.Should().Be("image/png");
    }
}

public class EmailSendResultTests
{
    [Fact]
    public void EmailSendResult_ShouldInitializeWithSuccessResult()
    {
        // Arrange & Act
        var result = new EmailSendResult(
            Success: true,
            Provider: "SendGrid",
            ProviderMessageId: "msg-123",
            Error: null
        );

        // Assert
        result.Success.Should().BeTrue();
        result.Provider.Should().Be("SendGrid");
        result.ProviderMessageId.Should().Be("msg-123");
        result.Error.Should().BeNull();
    }

    [Fact]
    public void EmailSendResult_ShouldInitializeWithFailureResult()
    {
        // Arrange & Act
        var result = new EmailSendResult(
            Success: false,
            Provider: "SMTP",
            ProviderMessageId: null,
            Error: "Connection timeout"
        );

        // Assert
        result.Success.Should().BeFalse();
        result.Provider.Should().Be("SMTP");
        result.ProviderMessageId.Should().BeNull();
        result.Error.Should().Be("Connection timeout");
    }

    [Fact]
    public void EmailSendResult_ShouldSupportRecordEquality()
    {
        // Arrange
        var result1 = new EmailSendResult(true, "SendGrid", "msg-123", null);
        var result2 = new EmailSendResult(true, "SendGrid", "msg-123", null);
        var result3 = new EmailSendResult(false, "SendGrid", "msg-123", "Error");

        // Assert
        result1.Should().Be(result2);
        result1.Should().NotBe(result3);
    }

    [Fact]
    public void EmailSendResult_WithoutProvider_ShouldAllowNullProvider()
    {
        // Arrange & Act
        var result = new EmailSendResult(
            Success: false,
            Provider: null,
            ProviderMessageId: null,
            Error: "No provider available"
        );

        // Assert
        result.Success.Should().BeFalse();
        result.Provider.Should().BeNull();
        result.Error.Should().Be("No provider available");
    }
}
