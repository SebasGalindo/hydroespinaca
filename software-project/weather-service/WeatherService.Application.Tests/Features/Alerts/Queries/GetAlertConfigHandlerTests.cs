using WeatherService.Application.Features.Alerts.Queries.GetAlertConfig;
using WeatherService.Domain.Entities;
using WeatherService.Domain.Interfaces;

namespace WeatherService.Application.Tests.Features.Alerts.Queries;

public class GetAlertConfigHandlerTests
{
    private readonly Mock<IWeatherAlertConfigRepository> _repositoryMock = new();
    private readonly GetAlertConfigHandler _handler;

    public GetAlertConfigHandlerTests()
    {
        _handler = new GetAlertConfigHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WhenExists_ReturnsConfig()
    {
        var config = new WeatherAlertConfig
        {
            FuzzySystemId = "fuzzy-1",
            FuzzySystemName = "Test System",
            IsActive = true
        };

        _repositoryMock
            .Setup(r => r.GetByFuzzySystemIdAsync("fuzzy-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        var result = await _handler.Handle(
            new GetAlertConfigQuery("fuzzy-1"),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.FuzzySystemId.Should().Be("fuzzy-1");
    }

    [Fact]
    public async Task Handle_WhenNotExists_ReturnsNull()
    {
        _repositoryMock
            .Setup(r => r.GetByFuzzySystemIdAsync("fuzzy-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((WeatherAlertConfig?)null);

        var result = await _handler.Handle(
            new GetAlertConfigQuery("fuzzy-1"),
            CancellationToken.None);

        result.Should().BeNull();
    }
}
