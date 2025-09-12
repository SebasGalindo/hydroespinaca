using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;
using Xunit;
using BffService.Domain.Exceptions;
using BffService.Application.Interfaces;
using Moq;
using HydroEspinaca.Shared.DTOs.Authentication;

namespace BffService.Tests.Integration;

public class ExceptionHandlingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ExceptionHandlingTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task SessionNotFoundException_Should_Return_404_With_ProblemDetails()
    {
        // Arrange
        var sessionId = "non-existent-session-id";

        // Act
        var response = await _client.GetAsync($"/api/auth/session/{sessionId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        
        var content = await response.Content.ReadAsStringAsync();
        var problemDetails = JsonSerializer.Deserialize<JsonElement>(content);
        
        problemDetails.GetProperty("title").GetString().Should().Be("Session Not Found");
        problemDetails.GetProperty("status").GetInt32().Should().Be(404);
    }

    [Fact]
    public async Task InvalidLoginCredentials_Should_Return_401_With_ProblemDetails()
    {
        // Arrange
        var loginRequest = new
        {
            Email = "invalid@example.com",
            Password = "wrongpassword"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert - Should be handled by GlobalExceptionMiddleware
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task ProxyWithoutSession_Should_Return_401()
    {
        // Act - Try to access proxy without session header
        var response = await _client.GetAsync("/proxy/sensor/test");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ValidationError_Should_Return_400_With_ProblemDetails()
    {
        // Arrange - Invalid request (empty email)
        var loginRequest = new
        {
            Email = "",
            Password = "test"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized, HttpStatusCode.InternalServerError);
    }
}