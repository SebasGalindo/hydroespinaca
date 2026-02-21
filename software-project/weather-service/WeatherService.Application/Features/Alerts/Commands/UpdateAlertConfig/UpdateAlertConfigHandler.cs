using MediatR;
using WeatherService.Domain.Entities;
using WeatherService.Domain.Interfaces;

namespace WeatherService.Application.Features.Alerts.Commands.UpdateAlertConfig;

/// <summary>
/// Handler for the UpdateAlertConfigCommand, responsible for updating an existing weather alert configuration
/// </summary>
public class UpdateAlertConfigHandler : IRequestHandler<UpdateAlertConfigCommand, WeatherAlertConfig>
{
    private readonly IWeatherAlertConfigRepository _repository;

    public UpdateAlertConfigHandler(IWeatherAlertConfigRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Handles the UpdateAlertConfigCommand by retrieving the existing alert configuration for the specified fuzzy system,
    /// updating its properties based on the command's data, and saving the updated configuration back to the repository. 
    /// It also updates the timestamp and user information for auditing purposes. If the
    /// </summary>
    /// <param name="request">The command containing the updated alert configuration data.</param>
    /// <param name="cancellationToken">The cancellation token to monitor for cancellation requests.</param>
    /// <returns>The updated WeatherAlertConfig entity.</returns>
    /// <exception cref="KeyNotFoundException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task<WeatherAlertConfig> Handle(UpdateAlertConfigCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByFuzzySystemIdAsync(request.FuzzySystemId, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"Alert config not found for fuzzy system '{request.FuzzySystemId}'");

        existing.IsActive = request.IsActive;
        existing.Alerts = request.Alerts.Select(a => new AlertThreshold
        {
            Type = a.Type,
            Enabled = a.Enabled,
            ThresholdValue = a.ThresholdValue,
            Comparison = a.Comparison,
            Recommendation = a.Recommendation
        }).ToList();
        existing.UpdateTimestamp(request.UserId);

        var updated = await _repository.UpdateAsync(request.FuzzySystemId, existing, cancellationToken)
            ?? throw new InvalidOperationException("Failed to update alert config");

        return updated;
    }
}
