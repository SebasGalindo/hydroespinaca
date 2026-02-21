using MediatR;
using WeatherService.Domain.Entities;
using WeatherService.Domain.Interfaces;

namespace WeatherService.Application.Features.Alerts.Queries.GetAlertConfig;

public class GetAlertConfigHandler : IRequestHandler<GetAlertConfigQuery, WeatherAlertConfig?>
{
    private readonly IWeatherAlertConfigRepository _repository;

    public GetAlertConfigHandler(IWeatherAlertConfigRepository repository)
    {
        _repository = repository;
    }

    public async Task<WeatherAlertConfig?> Handle(GetAlertConfigQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetByFuzzySystemIdAsync(request.FuzzySystemId, cancellationToken);
    }
}
