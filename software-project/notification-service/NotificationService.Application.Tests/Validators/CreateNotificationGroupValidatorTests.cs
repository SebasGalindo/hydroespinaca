using FluentAssertions;
using FluentValidation.TestHelper;
using NotificationService.Application.DTOs;
using NotificationService.Application.Validators;

namespace NotificationService.Application.Tests.Validators;

public class CreateNotificationGroupValidatorTests
{
    private readonly CreateNotificationGroupValidator _validator = new();

    [Fact]
    public void Validate_WithValidData_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var dto = new CreateNotificationGroupDto
        {
            GroupName = "Test Group",
            Description = "Test Description",
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
    public void Validate_WithEmptyGroupName_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new CreateNotificationGroupDto
        {
            GroupName = "",
            Recipients = new List<GroupRecipientDto>
            {
                new() { Email = "test@example.com", IsActive = true }
            }
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.GroupName)
            .WithErrorMessage("Group name is required");
    }

    [Theory]
    [InlineData("A")]
    [InlineData("Valid-Group_Name 123")]
    [InlineData("A" + "bcdefghijklmnopqrstuvwxyz0123456789")]
    public void Validate_WithValidGroupNameLength_ShouldNotHaveValidationError(string groupName)
    {
        // Arrange
        var dto = new CreateNotificationGroupDto
        {
            GroupName = groupName.Length > 100 ? groupName.Substring(0, 100) : groupName,
            Recipients = new List<GroupRecipientDto>
            {
                new() { Email = "test@example.com", IsActive = true }
            }
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.GroupName);
    }

    [Fact]
    public void Validate_WithTooLongGroupName_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new CreateNotificationGroupDto
        {
            GroupName = new string('a', 101),
            Recipients = new List<GroupRecipientDto>
            {
                new() { Email = "test@example.com", IsActive = true }
            }
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.GroupName)
            .WithErrorMessage("Group name must be between 1 and 100 characters");
    }

    [Theory]
    [InlineData("Group@Name")]
    [InlineData("Group#Name")]
    [InlineData("Group$Name")]
    [InlineData("Group%Name")]
    public void Validate_WithInvalidCharactersInGroupName_ShouldHaveValidationError(string invalidGroupName)
    {
        // Arrange
        var dto = new CreateNotificationGroupDto
        {
            GroupName = invalidGroupName,
            Recipients = new List<GroupRecipientDto>
            {
                new() { Email = "test@example.com", IsActive = true }
            }
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.GroupName)
            .WithErrorMessage("Group name can only contain letters, numbers, hyphens, underscores and spaces");
    }

    [Fact]
    public void Validate_WithTooLongDescription_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new CreateNotificationGroupDto
        {
            GroupName = "Test Group",
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
    public void Validate_WithEmptyRecipients_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new CreateNotificationGroupDto
        {
            GroupName = "Test Group",
            Recipients = new List<GroupRecipientDto>()
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Recipients)
            .WithErrorMessage("At least one recipient is required");
    }

    [Fact]
    public void Validate_WithNoActiveRecipients_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new CreateNotificationGroupDto
        {
            GroupName = "Test Group",
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
        var dto = new CreateNotificationGroupDto
        {
            GroupName = "Test Group",
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
    public void Validate_WithMultipleRecipients_OneActive_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = new CreateNotificationGroupDto
        {
            GroupName = "Test Group",
            Recipients = new List<GroupRecipientDto>
            {
                new() { Email = "active@example.com", IsActive = true },
                new() { Email = "inactive@example.com", IsActive = false }
            }
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
