using FluentValidation.TestHelper;
using HydroEspinaca.Shared.DTOs.Authentication;
using BffService.Application.Validators;

namespace BffService.Application.Tests.Validators;

public class LogoutRequestValidatorTests
{
    private readonly LogoutRequestValidator _validator = new();

    [Fact]
    public void Validate_WithValidSessionId_ShouldNotHaveError()
    {
        // Arrange
        var request = new LogoutRequestDto("valid-session-id");

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptySessionId_ShouldHaveError()
    {
        // Arrange
        var request = new LogoutRequestDto("");

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SessionId)
            .WithErrorMessage("Session ID es requerido");
    }

    [Fact]
    public void Validate_WithNullSessionId_ShouldHaveError()
    {
        // Arrange
        var request = new LogoutRequestDto(null!);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SessionId);
    }
}

public class RefreshTokenRequestValidatorTests
{
    private readonly RefreshTokenRequestValidator _validator = new();

    [Fact]
    public void Validate_WithValidSessionId_ShouldNotHaveError()
    {
        // Arrange
        var request = new RefreshTokenRequestDto("valid-session-id");

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptySessionId_ShouldHaveError()
    {
        // Arrange
        var request = new RefreshTokenRequestDto("");

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SessionId)
            .WithErrorMessage("Session ID es requerido");
    }

    [Fact]
    public void Validate_WithNullSessionId_ShouldHaveError()
    {
        // Arrange
        var request = new RefreshTokenRequestDto(null!);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SessionId);
    }
}
