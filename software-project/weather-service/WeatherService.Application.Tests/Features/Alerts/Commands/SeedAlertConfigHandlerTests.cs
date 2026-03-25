using WeatherService.Application.Features.Alerts.Commands.SeedAlertConfig;
using WeatherService.Domain.Entities;
using WeatherService.Domain.Enums;
using WeatherService.Domain.Interfaces;

namespace WeatherService.Application.Tests.Features.Alerts.Commands;

public class SeedAlertConfigHandlerTests
{
    private readonly Mock<IWeatherAlertConfigRepository> _repositoryMock = new();
    private readonly SeedAlertConfigHandler _handler;

    public SeedAlertConfigHandlerTests()
    {
        _handler = new SeedAlertConfigHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_CreatesConfigWithDefaultThresholds()
    {
        _repositoryMock
            .Setup(r => r.ExistsAsync("fuzzy-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _repositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<WeatherAlertConfig>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WeatherAlertConfig c, CancellationToken _) =>
            {
                c.SetId("config-1");
                return c;
            });

        var result = await _handler.Handle(
            new SeedAlertConfigCommand("fuzzy-1", "My System", "user-1"),
            CancellationToken.None);

        result.FuzzySystemId.Should().Be("fuzzy-1");
        result.FuzzySystemName.Should().Be("My System");
        result.CreatedBy.Should().Be("user-1");
        result.IsActive.Should().BeTrue();
        result.Alerts.Should().HaveCount(AlertTypes.All.Length);
    }

    [Fact]
    public async Task Handle_WhenAlreadyExists_ThrowsInvalidOperationException()
    {
        _repositoryMock
            .Setup(r => r.ExistsAsync("fuzzy-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var act = () => _handler.Handle(
            new SeedAlertConfigCommand("fuzzy-1", "My System", "user-1"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task Handle_DefaultThresholds_IncludeAllAlertTypes()
    {
        _repositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _repositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<WeatherAlertConfig>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WeatherAlertConfig c, CancellationToken _) => c);

        var result = await _handler.Handle(
            new SeedAlertConfigCommand("fuzzy-1", "Test", "user-1"),
            CancellationToken.None);

        var alertTypes = result.Alerts.Select(a => a.Type).ToList();
        foreach (var type in AlertTypes.All)
        {
            alertTypes.Should().Contain(type);
        }
    }
}
