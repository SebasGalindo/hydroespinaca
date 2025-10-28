using FluentValidation.TestHelper;
using HydroEspinaca.Shared.DTOs.Notifications;
using NotificationService.Application.Validators;

namespace NotificationService.Application.Tests.Validators;

public class SendEmailRequestValidatorTests
{
    private readonly SendEmailRequestValidator _validator = new();

    #region Group vs To Validation

    [Fact]
    public void Validate_WithBothGroupAndTo_ShouldHaveError()
    {
        // Arrange
        var request = new SendEmailRequestDto
        {
            Group = "test-group",
            To = "test@example.com",
            Subject = "Test",
            HtmlBody = "<p>Test</p>"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("Cannot specify both 'Group' and 'To' fields. Choose one sending mode.");
    }

    [Fact]
    public void Validate_WithNeitherGroupNorTo_ShouldHaveError()
    {
        // Arrange
        var request = new SendEmailRequestDto
        {
            Subject = "Test",
            HtmlBody = "<p>Test</p>"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("Either 'Group' or 'To' field must be specified");
    }

    #endregion

    #region Direct Send Mode (To) Validation

    [Fact]
    public void Validate_WithValidDirectSendMode_ShouldNotHaveError()
    {
        // Arrange
        var request = new SendEmailRequestDto
        {
            To = "test@example.com",
            Subject = "Test Subject",
            HtmlBody = "<p>Test Body</p>"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithInvalidToEmail_ShouldHaveError()
    {
        // Arrange
        var request = new SendEmailRequestDto
        {
            To = "invalid-email",
            Subject = "Test",
            HtmlBody = "<p>Test</p>"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.To);
    }

    [Fact]
    public void Validate_WithInvalidCcEmail_ShouldHaveError()
    {
        // Arrange
        var request = new SendEmailRequestDto
        {
            To = "test@example.com",
            Cc = new[] { "invalid-email" },
            Subject = "Test",
            HtmlBody = "<p>Test</p>"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor("Cc[0]");
    }

    [Fact]
    public void Validate_WithInvalidBccEmail_ShouldHaveError()
    {
        // Arrange
        var request = new SendEmailRequestDto
        {
            To = "test@example.com",
            Bcc = new[] { "invalid-email" },
            Subject = "Test",
            HtmlBody = "<p>Test</p>"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor("Bcc[0]");
    }

    #endregion

    #region Group Send Mode Validation

    [Fact]
    public void Validate_WithValidGroupMode_ShouldNotHaveError()
    {
        // Arrange
        var request = new SendEmailRequestDto
        {
            Group = "valid-group",
            Subject = "Test Subject",
            HtmlBody = "<p>Test Body</p>"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyGroup_ShouldHaveError()
    {
        // Arrange
        var request = new SendEmailRequestDto
        {
            Group = "",
            Subject = "Test",
            HtmlBody = "<p>Test</p>"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("Either 'Group' or 'To' field must be specified");
    }

    [Fact]
    public void Validate_WithGroupTooLong_ShouldHaveError()
    {
        // Arrange
        var request = new SendEmailRequestDto
        {
            Group = new string('a', 101), // 101 characters
            Subject = "Test",
            HtmlBody = "<p>Test</p>"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Group);
    }

    [Fact]
    public void Validate_WithInvalidGroupCharacters_ShouldHaveError()
    {
        // Arrange
        var request = new SendEmailRequestDto
        {
            Group = "invalid@group!",
            Subject = "Test",
            HtmlBody = "<p>Test</p>"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Group)
            .WithErrorMessage("Group name can only contain letters, numbers, hyphens, underscores and spaces");
    }

    [Fact]
    public void Validate_WithGroupAndCc_ShouldHaveError()
    {
        // Arrange
        var request = new SendEmailRequestDto
        {
            Group = "test-group",
            Cc = new[] { "cc@example.com" },
            Subject = "Test",
            HtmlBody = "<p>Test</p>"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Cc)
            .WithErrorMessage("Cannot specify 'Cc' when using 'Group' mode");
    }

    [Fact]
    public void Validate_WithGroupAndBcc_ShouldHaveError()
    {
        // Arrange
        var request = new SendEmailRequestDto
        {
            Group = "test-group",
            Bcc = new[] { "bcc@example.com" },
            Subject = "Test",
            HtmlBody = "<p>Test</p>"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Bcc)
            .WithErrorMessage("Cannot specify 'Bcc' when using 'Group' mode");
    }

    #endregion

    #region Subject and HtmlBody Validation

    [Fact]
    public void Validate_WithEmptySubject_ShouldHaveError()
    {
        // Arrange
        var request = new SendEmailRequestDto
        {
            To = "test@example.com",
            Subject = "",
            HtmlBody = "<p>Test</p>"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Subject);
    }

    [Fact]
    public void Validate_WithSubjectTooLong_ShouldHaveError()
    {
        // Arrange
        var request = new SendEmailRequestDto
        {
            To = "test@example.com",
            Subject = new string('a', 201), // 201 characters
            HtmlBody = "<p>Test</p>"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Subject);
    }

    [Fact]
    public void Validate_WithEmptyHtmlBody_ShouldHaveError()
    {
        // Arrange
        var request = new SendEmailRequestDto
        {
            To = "test@example.com",
            Subject = "Test",
            HtmlBody = ""
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.HtmlBody);
    }

    [Fact]
    public void Validate_WithHtmlBodyTooLarge_ShouldHaveError()
    {
        // Arrange
        var request = new SendEmailRequestDto
        {
            To = "test@example.com",
            Subject = "Test",
            HtmlBody = new string('a', 100_001) // 100KB + 1 byte
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.HtmlBody);
    }

    #endregion

    #region Attachments Validation

    [Fact]
    public void Validate_WithValidAttachment_ShouldNotHaveError()
    {
        // Arrange
        var validBase64 = Convert.ToBase64String(new byte[] { 1, 2, 3, 4, 5 });
        var request = new SendEmailRequestDto
        {
            To = "test@example.com",
            Subject = "Test",
            HtmlBody = "<p>Test</p>",
            Attachments = new List<AttachmentDto>
            {
                new() { FileName = "test.pdf", ContentBase64 = validBase64, ContentType = "application/pdf" }
            }
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithInvalidBase64_ShouldHaveError()
    {
        // Arrange
        var request = new SendEmailRequestDto
        {
            To = "test@example.com",
            Subject = "Test",
            HtmlBody = "<p>Test</p>",
            Attachments = new List<AttachmentDto>
            {
                new() { FileName = "test.pdf", ContentBase64 = "not-valid-base64!!!", ContentType = "application/pdf" }
            }
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor("Attachments[0].ContentBase64")
            .WithErrorMessage("Attachment content must be Base64");
    }

    [Fact]
    public void Validate_WithAttachmentFileNameTooLong_ShouldHaveError()
    {
        // Arrange
        var validBase64 = Convert.ToBase64String(new byte[] { 1, 2, 3 });
        var request = new SendEmailRequestDto
        {
            To = "test@example.com",
            Subject = "Test",
            HtmlBody = "<p>Test</p>",
            Attachments = new List<AttachmentDto>
            {
                new() { FileName = new string('a', 256), ContentBase64 = validBase64, ContentType = "application/pdf" }
            }
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor("Attachments[0].FileName");
    }

    [Fact]
    public void Validate_WithEmptyFileName_ShouldHaveError()
    {
        // Arrange
        var validBase64 = Convert.ToBase64String(new byte[] { 1, 2, 3 });
        var request = new SendEmailRequestDto
        {
            To = "test@example.com",
            Subject = "Test",
            HtmlBody = "<p>Test</p>",
            Attachments = new List<AttachmentDto>
            {
                new() { FileName = "", ContentBase64 = validBase64, ContentType = "application/pdf" }
            }
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor("Attachments[0].FileName");
    }

    [Fact]
    public void Validate_WithEmptyContentType_ShouldHaveError()
    {
        // Arrange
        var validBase64 = Convert.ToBase64String(new byte[] { 1, 2, 3 });
        var request = new SendEmailRequestDto
        {
            To = "test@example.com",
            Subject = "Test",
            HtmlBody = "<p>Test</p>",
            Attachments = new List<AttachmentDto>
            {
                new() { FileName = "test.pdf", ContentBase64 = validBase64, ContentType = "" }
            }
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor("Attachments[0].ContentType");
    }

    #endregion
}
