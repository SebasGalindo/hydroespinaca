using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Xunit;
using BffService.Api.Services;
using BffService.Domain.Exceptions;
using FluentValidation;
using HydroEspinaca.Shared.Errors;

namespace BffService.Tests.Unit.Services;

public class BffServiceExceptionMapperTests
{
    private readonly BffServiceExceptionMapper _mapper;
    private readonly ProblemDetailsFactory _sharedMapper;

    public BffServiceExceptionMapperTests()
    {
        _sharedMapper = new ProblemDetailsFactory();
        _mapper = new BffServiceExceptionMapper(_sharedMapper);
    }

    [Fact]
    public void CanHandle_SessionNotFoundException_Should_Return_True()
    {
        // Arrange
        var exception = new SessionNotFoundException("test-session-id");

        // Act
        var canHandle = _mapper.CanHandle(exception);

        // Assert
        canHandle.Should().BeTrue();
    }

    [Fact]
    public void GetStatusCode_SessionNotFoundException_Should_Return_404()
    {
        // Arrange
        var exception = new SessionNotFoundException("test-session-id");

        // Act
        var statusCode = _mapper.GetStatusCode(exception);

        // Assert
        statusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public void MapToProblemDetails_SessionNotFoundException_Should_Return_Correct_ProblemDetails()
    {
        // Arrange
        var exception = new SessionNotFoundException("test-session-id");
        var requestPath = "/api/auth/session/test-session-id";
        var isDevelopment = true;

        // Act
        var problemDetails = _mapper.MapToProblemDetails(exception, requestPath, isDevelopment);

        // Assert
        problemDetails.Title.Should().Be("Session Not Found");
        problemDetails.Status.Should().Be(StatusCodes.Status404NotFound);
        problemDetails.Instance.Should().Be(requestPath);
        problemDetails.Type.Should().Be("https://tools.ietf.org/html/rfc7231#section-6.5.4");
        problemDetails.Detail.Should().Contain("test-session-id");
    }

    [Fact]
    public void MapToProblemDetails_InvalidTokenException_Should_Hide_Details_In_Production()
    {
        // Arrange
        var exception = new InvalidTokenException("Detailed error message");
        var requestPath = "/api/auth/login";
        var isDevelopment = false; // Production mode

        // Act
        var problemDetails = _mapper.MapToProblemDetails(exception, requestPath, isDevelopment);

        // Assert
        problemDetails.Title.Should().Be("Invalid Token");
        problemDetails.Status.Should().Be(StatusCodes.Status401Unauthorized);
        problemDetails.Detail.Should().Be("Authentication required");
        problemDetails.Detail.Should().NotContain("Detailed error message");
    }

    [Fact]
    public void MapToProblemDetails_InvalidTokenException_Should_Show_Details_In_Development()
    {
        // Arrange
        var exception = new InvalidTokenException("Detailed error message");
        var requestPath = "/api/auth/login";
        var isDevelopment = true; // Development mode

        // Act
        var problemDetails = _mapper.MapToProblemDetails(exception, requestPath, isDevelopment);

        // Assert
        problemDetails.Detail.Should().Be("Detailed error message");
    }

    [Fact]
    public void MapToProblemDetails_ValidationException_Should_Return_400()
    {
        // Arrange
        var exception = new ValidationException("Validation failed");
        var requestPath = "/api/auth/login";

        // Act
        var problemDetails = _mapper.MapToProblemDetails(exception, requestPath, true);

        // Assert
        problemDetails.Title.Should().Be("Validation Error");
        problemDetails.Status.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public void MapToProblemDetails_UnknownException_Should_Return_500_Fallback()
    {
        // Arrange
        var exception = new InvalidOperationException("Unknown error");
        var requestPath = "/api/test";
        var isDevelopment = false;

        // Act
        var problemDetails = _mapper.MapToProblemDetails(exception, requestPath, isDevelopment);

        // Assert
        problemDetails.Title.Should().Be("Internal Server Error");
        problemDetails.Status.Should().Be(StatusCodes.Status500InternalServerError);
        problemDetails.Detail.Should().Be("An error occurred in BFF service");
    }
}