using WeatherService.Application.Features.Alerts.Queries.GetAlerts;
using WeatherService.Domain.Entities;
using WeatherService.Domain.Interfaces;

namespace WeatherService.Application.Tests.Features.Alerts.Queries;

public class GetAlertsHandlerTests
{
    private readonly Mock<IWeatherAlertRepository> _repositoryMock = new();
    private readonly GetAlertsHandler _handler;

    public GetAlertsHandlerTests()
    {
        _handler = new GetAlertsHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ReturnsAlerts()
    {
        var alerts = new List<WeatherAlert>
        {
            new() { FuzzySystemId = "fuzzy-1", AlertType = "extreme_heat" },
            new() { FuzzySystemId = "fuzzy-1", AlertType = "heavy_rain" }
        };

        _repositoryMock
            .Setup(r => r.GetByFiltersAsync(
                "fuzzy-1", null, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(alerts);

        var result = await _handler.Handle(
            new GetAlertsQuery("fuzzy-1"),
            CancellationToken.None);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_PassesAllFilters()
    {
        var from = DateTime.UtcNow.AddDays(-7);
        var to = DateTime.UtcNow;

        _repositoryMock
            .Setup(r => r.GetByFiltersAsync(
                "fuzzy-1", "user-1", from, to, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WeatherAlert>());

        await _handler.Handle(
            new GetAlertsQuery("fuzzy-1", "user-1", from, to, true),
            CancellationToken.None);

        _repositoryMock.Verify(
            r => r.GetByFiltersAsync("fuzzy-1", "user-1", from, to, true, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_EmptyResult_ReturnsEmptyList()
    {
        _repositoryMock
            .Setup(r => r.GetByFiltersAsync(
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(), It.IsAny<bool?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WeatherAlert>());

        var result = await _handler.Handle(new GetAlertsQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }
}
