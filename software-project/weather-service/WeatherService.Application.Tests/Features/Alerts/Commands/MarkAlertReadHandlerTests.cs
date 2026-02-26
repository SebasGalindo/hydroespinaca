using WeatherService.Application.Features.Alerts.Commands.MarkAlertRead;
using WeatherService.Domain.Interfaces;

namespace WeatherService.Application.Tests.Features.Alerts.Commands;

public class MarkAlertReadHandlerTests
{
    private readonly Mock<IWeatherAlertRepository> _repositoryMock = new();
    private readonly MarkAlertReadHandler _handler;

    public MarkAlertReadHandlerTests()
    {
        _handler = new MarkAlertReadHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WhenSuccessful_ReturnsTrue()
    {
        _repositoryMock
            .Setup(r => r.MarkAsReadAsync("alert-1", "user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _handler.Handle(
            new MarkAlertReadCommand("alert-1", "user-1"),
            CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenNotFound_ThrowsKeyNotFoundException()
    {
        _repositoryMock
            .Setup(r => r.MarkAsReadAsync("alert-1", "user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var act = () => _handler.Handle(
            new MarkAlertReadCommand("alert-1", "user-1"),
            CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_PassesCorrectParametersToRepository()
    {
        _repositoryMock
            .Setup(r => r.MarkAsReadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _handler.Handle(
            new MarkAlertReadCommand("alert-xyz", "user-abc"),
            CancellationToken.None);

        _repositoryMock.Verify(
            r => r.MarkAsReadAsync("alert-xyz", "user-abc", It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
