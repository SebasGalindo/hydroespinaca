using FluentValidation.TestHelper;
using NotificationService.Application.DTOs;
using NotificationService.Application.Validators;

namespace NotificationService.Application.Tests.Validators;

public class GroupRecipientValidatorTests
{
    private readonly GroupRecipientValidator _validator = new();

    [Fact]
    public void Validate_WithValidEmail_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var dto = new GroupRecipientDto
        {
            Email = "test@example.com",
            Type = RecipientTypeDto.TO,
            IsActive = true
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyEmail_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new GroupRecipientDto
        {
            Email = "",
            Type = RecipientTypeDto.TO,
            IsActive = true
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Email is required");
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("@example.com")]
    [InlineData("test@")]
    [InlineData("test")]
    public void Validate_WithInvalidEmailFormat_ShouldHaveValidationError(string invalidEmail)
    {
        // Arrange
        var dto = new GroupRecipientDto
        {
            Email = invalidEmail,
            Type = RecipientTypeDto.TO,
            IsActive = true
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Email must be a valid email address");
    }

    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name@example.com")]
    [InlineData("user+tag@example.co.uk")]
    [InlineData("test123@test-domain.com")]
    public void Validate_WithValidEmailFormats_ShouldNotHaveValidationError(string validEmail)
    {
        // Arrange
        var dto = new GroupRecipientDto
        {
            Email = validEmail,
            Type = RecipientTypeDto.TO,
            IsActive = true
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData(RecipientTypeDto.TO)]
    [InlineData(RecipientTypeDto.CC)]
    [InlineData(RecipientTypeDto.BCC)]
    public void Validate_WithValidRecipientType_ShouldNotHaveValidationError(RecipientTypeDto type)
    {
        // Arrange
        var dto = new GroupRecipientDto
        {
            Email = "test@example.com",
            Type = type,
            IsActive = true
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Type);
    }

    [Fact]
    public void Validate_WithInvalidRecipientType_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new GroupRecipientDto
        {
            Email = "test@example.com",
            Type = (RecipientTypeDto)999, // Invalid enum value
            IsActive = true
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Type)
            .WithErrorMessage("Invalid recipient type");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Validate_WithAnyIsActiveValue_ShouldNotHaveValidationError(bool isActive)
    {
        // Arrange
        var dto = new GroupRecipientDto
        {
            Email = "test@example.com",
            Type = RecipientTypeDto.TO,
            IsActive = isActive
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
