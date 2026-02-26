using ChatbotService.Application.Features.Chat.Queries.GetUserSessions;
using ChatbotService.Domain.Entities;
using ChatbotService.Domain.Interfaces;

namespace ChatbotService.Application.Tests.Features.Chat;

public class GetUserSessionsQueryHandlerTests
{
    private readonly Mock<IChatSessionRepository> _sessionRepoMock;
    private readonly GetUserSessionsQueryHandler _handler;

    public GetUserSessionsQueryHandlerTests()
    {
        _sessionRepoMock = new Mock<IChatSessionRepository>();
        _handler = new GetUserSessionsQueryHandler(_sessionRepoMock.Object);
    }

    [Fact]
    public async Task Handle_UserWithSessions_ShouldReturnDtoList()
    {
        // Arrange
        var sessions = new List<ChatSession>
        {
            new() { UserId = "user-1", Title = "Sesión A", CreatedAt = DateTime.UtcNow.AddHours(-2), UpdatedAt = DateTime.UtcNow },
            new() { UserId = "user-1", Title = "Sesión B", CreatedAt = DateTime.UtcNow.AddDays(-1) }
        };
        sessions[0].SetId("s-1");
        sessions[1].SetId("s-2");

        _sessionRepoMock
            .Setup(x => x.GetSessionsByUserAsync("user-1", 0, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessions);

        var query = new GetUserSessionsQuery("user-1");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result[0].Id.Should().Be("s-1");
        result[0].Title.Should().Be("Sesión A");
        result[1].Id.Should().Be("s-2");
        result[1].Title.Should().Be("Sesión B");
    }

    [Fact]
    public async Task Handle_UserWithNoSessions_ShouldReturnEmptyList()
    {
        // Arrange
        _sessionRepoMock
            .Setup(x => x.GetSessionsByUserAsync("user-new", 0, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChatSession>());

        var query = new GetUserSessionsQuery("user-new");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithPagination_ShouldPassSkipAndLimit()
    {
        // Arrange
        _sessionRepoMock
            .Setup(x => x.GetSessionsByUserAsync("user-1", 10, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChatSession>());

        var query = new GetUserSessionsQuery("user-1", Skip: 10, Limit: 5);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _sessionRepoMock.Verify(
            x => x.GetSessionsByUserAsync("user-1", 10, 5, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
