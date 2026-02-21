using MediatR;
using WeatherService.Domain.Entities;
using WeatherService.Domain.Interfaces;

namespace WeatherService.Application.Features.Alerts.Queries.GetAlerts;

public class GetAlertsHandler : IRequestHandler<GetAlertsQuery, List<WeatherAlert>>
{
    private readonly IWeatherAlertRepository _repository;

    public GetAlertsHandler(IWeatherAlertRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<WeatherAlert>> Handle(GetAlertsQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetByFiltersAsync(
            request.FuzzySystemId,
            request.UserId,
            request.From,
            request.To,
            request.UnreadOnly,
            cancellationToken);
    }
}
