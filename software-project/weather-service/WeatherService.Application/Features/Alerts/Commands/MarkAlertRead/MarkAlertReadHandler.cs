using MediatR;
using WeatherService.Domain.Interfaces;

namespace WeatherService.Application.Features.Alerts.Commands.MarkAlertRead;

public class MarkAlertReadHandler : IRequestHandler<MarkAlertReadCommand, bool>
{
    private readonly IWeatherAlertRepository _repository;

    public MarkAlertReadHandler(IWeatherAlertRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(MarkAlertReadCommand request, CancellationToken cancellationToken)
    {
        var result = await _repository.MarkAsReadAsync(request.AlertId, request.UserId, cancellationToken);
        if (!result)
        {
            throw new KeyNotFoundException(
                $"Alert '{request.AlertId}' not found or user '{request.UserId}' not in notified list");
        }
        return true;
    }
}
