using BiService.Application.DTOs.OperationalCost;
using BiService.Domain.Entities;
using BiService.Domain.Interfaces;
using MediatR;

namespace BiService.Application.Features.OperationalCost.Queries.CalculateOperationalCost;

/// <summary>
/// Handler que calcula el costo operacional estimado por consumo eléctrico de actuadores.
/// Segmenta los costos por periodo según las versiones de configuración de costos vigentes
/// y distribuye el consumo proporcionalmente.
/// </summary>
public class CalculateOperationalCostQueryHandler
    : IRequestHandler<CalculateOperationalCostQuery, OperationalCostResponse>
{
    private readonly ICostConfigVersionRepository _costConfigRepository;

    public CalculateOperationalCostQueryHandler(ICostConfigVersionRepository costConfigRepository)
        => _costConfigRepository = costConfigRepository;

    public async Task<OperationalCostResponse> Handle(
        CalculateOperationalCostQuery request, CancellationToken cancellationToken)
    {
        var versions = await _costConfigRepository.GetVersionsForRangeAsync(
            request.From, request.To, cancellationToken);

        if (versions.Count == 0)
        {
            // Fallback: try to get the current active config
            var current = await _costConfigRepository.GetCurrentAsync(cancellationToken);
            if (current is not null)
                versions = new List<CostConfigVersion> { current };
        }

        if (versions.Count == 0)
            throw new InvalidOperationException(
                "No existe una configuración de costos para el rango de fechas solicitado.");

        // Build period breakdown with proportions
        var periods = BuildPeriods(versions, request.From, request.To);
        var totalDays = (request.To - request.From).TotalDays;
        if (totalDays <= 0) totalDays = 1;

        var actuatorResults = new List<ActuatorOperationalCostItem>();

        foreach (var actuator in request.ActuatorDurations)
        {
            var totalHours = (decimal)actuator.TotalDurationSeconds / 3600m;
            var totalKwh = 0m;
            var totalCost = 0m;

            foreach (var period in periods)
            {
                var proportion = (decimal)period.Days / (decimal)totalDays;
                var periodDurationSeconds = (decimal)actuator.TotalDurationSeconds * proportion;
                var periodHours = periodDurationSeconds / 3600m;
                var periodKwh = periodHours * actuator.PowerConsumptionWatts / 1000m;
                var periodCost = periodKwh * period.ElectricityCostPerKwh;

                totalKwh += periodKwh;
                totalCost += periodCost;
            }

            actuatorResults.Add(new ActuatorOperationalCostItem
            {
                ActuatorCode = actuator.ActuatorCode,
                PowerConsumptionWatts = actuator.PowerConsumptionWatts,
                TotalDurationSeconds = actuator.TotalDurationSeconds,
                TotalHours = decimal.Round(totalHours, 4),
                EstimatedKwh = decimal.Round(totalKwh, 4),
                EstimatedCost = decimal.Round(totalCost, 4),
                ActivationCount = actuator.ActivationCount
            });
        }

        var currency = versions.First().Currency;

        return new OperationalCostResponse
        {
            From = request.From,
            To = request.To,
            Currency = currency,
            CostConfigPeriodsUsed = periods.Select(p => new CostConfigPeriodUsed
            {
                VersionId = p.VersionId,
                From = p.From,
                To = p.To,
                ElectricityCostPerKwh = p.ElectricityCostPerKwh,
                Currency = p.Currency
            }).ToList(),
            Actuators = actuatorResults,
            TotalEstimatedKwh = actuatorResults.Sum(a => a.EstimatedKwh),
            TotalOperationalCost = actuatorResults.Sum(a => a.EstimatedCost)
        };
    }

    private static List<PeriodInfo> BuildPeriods(
        IReadOnlyList<CostConfigVersion> versions, DateTime rangeFrom, DateTime rangeTo)
    {
        var periods = new List<PeriodInfo>();

        foreach (var version in versions)
        {
            // Determine the overlap of [version.EffectiveFrom, version.EffectiveTo] with [rangeFrom, rangeTo]
            var periodStart = version.EffectiveFrom > rangeFrom ? version.EffectiveFrom : rangeFrom;
            var periodEnd = version.EffectiveTo.HasValue && version.EffectiveTo.Value < rangeTo
                ? version.EffectiveTo.Value
                : rangeTo;

            if (periodStart >= periodEnd) continue;

            var days = (periodEnd - periodStart).TotalDays;
            if (days <= 0) days = 1;

            periods.Add(new PeriodInfo
            {
                VersionId = version.Id,
                From = periodStart,
                To = periodEnd,
                Days = days,
                ElectricityCostPerKwh = version.ElectricityCostPerKwh,
                Currency = version.Currency
            });
        }

        // If no periods were built (edge case), use the last version for the whole range
        if (periods.Count == 0 && versions.Count > 0)
        {
            var lastVersion = versions.Last();
            periods.Add(new PeriodInfo
            {
                VersionId = lastVersion.Id,
                From = rangeFrom,
                To = rangeTo,
                Days = Math.Max((rangeTo - rangeFrom).TotalDays, 1),
                ElectricityCostPerKwh = lastVersion.ElectricityCostPerKwh,
                Currency = lastVersion.Currency
            });
        }

        return periods;
    }

    private class PeriodInfo
    {
        public string VersionId { get; set; } = string.Empty;
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public double Days { get; set; }
        public decimal ElectricityCostPerKwh { get; set; }
        public string Currency { get; set; } = "COP";
    }
}
