using WeatherService.Application.Features.Alerts.Commands.UpdateAlertConfig;
using WeatherService.Domain.Entities;
using WeatherService.Domain.Interfaces;

namespace WeatherService.Application.Tests.Features.Alerts.Commands;

public class UpdateAlertConfigHandlerTests
{
    private readonly Mock<IWeatherAlertConfigRepository> _repositoryMock = new();
    private readonly UpdateAlertConfigHandler _handler;

    public UpdateAlertConfigHandlerTests()
    {
        _handler = new UpdateAlertConfigHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_UpdatesConfigSuccessfully()
    {
        var existing = new WeatherAlertConfig
        {
            FuzzySystemId = "fuzzy-1",
            IsActive = true,
            Alerts = new List<AlertThreshold>()
        };

        _repositoryMock
            .Setup(r => r.GetByFuzzySystemIdAsync("fuzzy-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        _repositoryMock
            .Setup(r => r.UpdateAsync("fuzzy-1", It.IsAny<WeatherAlertConfig>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, WeatherAlertConfig c, CancellationToken _) => c);

        var alerts = new List<AlertThresholdDto>
        {
            new("extreme_heat", true, 40, "gt", "Cool plants")
        };

        var result = await _handler.Handle(
            new UpdateAlertConfigCommand("fuzzy-1", "user-1", false, alerts),
            CancellationToken.None);

        result.IsActive.Should().BeFalse();
        result.Alerts.Should().HaveCount(1);
        result.Alerts[0].Type.Should().Be("extreme_heat");
        result.Alerts[0].ThresholdValue.Should().Be(40);
    }

    [Fact]
    public async Task Handle_WhenNotFound_ThrowsKeyNotFoundException()
    {
        _repositoryMock
            .Setup(r => r.GetByFuzzySystemIdAsync("fuzzy-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((WeatherAlertConfig?)null);

        var act = () => _handler.Handle(
            new UpdateAlertConfigCommand("fuzzy-1", "user-1", true, new List<AlertThresholdDto>()),
            CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenUpdateFails_ThrowsInvalidOperationException()
    {
        var existing = new WeatherAlertConfig { FuzzySystemId = "fuzzy-1" };

        _repositoryMock
            .Setup(r => r.GetByFuzzySystemIdAsync("fuzzy-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        _repositoryMock
            .Setup(r => r.UpdateAsync("fuzzy-1", It.IsAny<WeatherAlertConfig>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WeatherAlertConfig?)null);

        var act = () => _handler.Handle(
            new UpdateAlertConfigCommand("fuzzy-1", "user-1", true, new List<AlertThresholdDto>()),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_UpdatesTimestamp()
    {
        var existing = new WeatherAlertConfig { FuzzySystemId = "fuzzy-1" };
        var before = DateTime.UtcNow;

        _repositoryMock
            .Setup(r => r.GetByFuzzySystemIdAsync("fuzzy-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        _repositoryMock
            .Setup(r => r.UpdateAsync("fuzzy-1", It.IsAny<WeatherAlertConfig>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, WeatherAlertConfig c, CancellationToken _) => c);

        var result = await _handler.Handle(
            new UpdateAlertConfigCommand("fuzzy-1", "user-42", true, new List<AlertThresholdDto>()),
            CancellationToken.None);

        result.UpdatedBy.Should().Be("user-42");
        result.UpdatedAt.Should().BeOnOrAfter(before);
    }
}
