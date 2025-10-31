using FluentAssertions;
using FluentValidation.TestHelper;
using NotificationService.Application.DTOs;
using NotificationService.Application.Validators;

namespace NotificationService.Application.Tests.Validators;

public class UpdateNotificationGroupValidatorTests
{
    private readonly UpdateNotificationGroupValidator _validator = new();

    [Fact]
    public void Validate_WithValidData_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var dto = new UpdateNotificationGroupDto
        {
            Description = "Updated Description",
            Recipients = new List<GroupRecipientDto>
            {
                new() { Email = "test@example.com", Type = RecipientTypeDto.TO, IsActive = true }
            }
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithNullDescription_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var dto = new UpdateNotificationGroupDto
        {
            Description = null,
            Recipients = new List<GroupRecipientDto>
            {
                new() { Email = "test@example.com", IsActive = true }
            }
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Validate_WithTooLongDescription_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new UpdateNotificationGroupDto
        {
            Description = new string('a', 501),
            Recipients = new List<GroupRecipientDto>
            {
                new() { Email = "test@example.com", IsActive = true }
            }
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Description cannot exceed 500 characters");
    }

    [Fact]
    public void Validate_WithNullRecipients_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var dto = new UpdateNotificationGroupDto
        {
            Description = "Test",
            Recipients = null
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Recipients);
    }

    [Fact]
    public void Validate_WithEmptyRecipients_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var dto = new UpdateNotificationGroupDto
        {
            Description = "Test",
            Recipients = new List<GroupRecipientDto>()
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Recipients);
    }

    [Fact]
    public void Validate_WithNoActiveRecipients_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new UpdateNotificationGroupDto
        {
            Recipients = new List<GroupRecipientDto>
            {
                new() { Email = "test1@example.com", IsActive = false },
                new() { Email = "test2@example.com", IsActive = false }
            }
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Recipients)
            .WithErrorMessage("At least one recipient must be active");
    }

    [Fact]
    public void Validate_WithInvalidRecipientEmail_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new UpdateNotificationGroupDto
        {
            Recipients = new List<GroupRecipientDto>
            {
                new() { Email = "invalid-email", IsActive = true }
            }
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor("Recipients[0].Email");
    }

    [Fact]
    public void Validate_WithValidRecipients_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var dto = new UpdateNotificationGroupDto
        {
            Recipients = new List<GroupRecipientDto>
            {
                new() { Email = "active@example.com", Type = RecipientTypeDto.TO, IsActive = true },
                new() { Email = "inactive@example.com", Type = RecipientTypeDto.CC, IsActive = false }
            }
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithOnlyDescriptionUpdate_ShouldBeValid()
    {
        // Arrange
        var dto = new UpdateNotificationGroupDto
        {
            Description = "Only updating description",
            Recipients = null
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithOnlyRecipientsUpdate_ShouldBeValid()
    {
        // Arrange
        var dto = new UpdateNotificationGroupDto
        {
            Description = null,
            Recipients = new List<GroupRecipientDto>
            {
                new() { Email = "test@example.com", IsActive = true }
            }
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
