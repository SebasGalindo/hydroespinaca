using FluentValidation;
using HydroEspinaca.Shared.Errors;
using MongoDB.Bson;
using SensorService.Application.DTOs.Aggregate;
using SensorService.Application.Interfaces;
using SensorService.Application.Mappers;
using SensorService.Domain.Entities;
using SensorService.Domain.Interfaces;
using SensorService.Domain.Exceptions;
using HydroEspinaca.Shared.DTOs.Analytics;

namespace SensorService.Application.Services;

public class AggregateService : IAggregateService
{
    private readonly IAggregateRepository _repo;
    private readonly ISensorRepository _sensorRepo;
    private readonly IVariableRepository _variableRepo;

    public AggregateService(
        IAggregateRepository repo,
        ISensorRepository sensorRepo,
        IVariableRepository variableRepo
        )
    {
        _repo = repo;
        _sensorRepo = sensorRepo;
        _variableRepo = variableRepo;
    }

    public async Task<List<AggregateDto>> GetBySensorAndVariableAsync(string sensorId, string variableId, DateTime from, DateTime to)
    {
        if (!ObjectId.TryParse(sensorId, out _))
            throw new ValidationException("Formato de ID no válido. Se esperaba una cadena hexadecimal de 24 caracteres.");

        if (from > to)
            throw new ValidationException("La fecha inicial no puede ser mayor a la final.");

        var sensor = await _sensorRepo.GetByIdAsync(sensorId);
        if (sensor is null)
            throw new SensorNotFoundException(sensorId);

        var variable = await _variableRepo.GetByIdAsync(variableId);
        if (variable is null)
            throw new SensorDataNotFoundException($"Variable con ID no encontrada");

        var results = await _repo.GetBySensorAndVariableAsync(sensorId, variableId, from, to);
        return results.Select(AggregateMapper.ToDto).ToList();
    }

    public async Task<EnvironmentalAggregatesResponse> GetEnvironmentalAggregatesAsync(EnvironmentalAnalyticsRequest request)
    {
        // Normalize dates to UTC midnight to handle timezone offsets from frontend
        // Frontend may send dates like "2025-10-09T05:00:00.000Z" (midnight in Colombia UTC-5)
        // We normalize to "2025-10-09T00:00:00.000Z" (midnight UTC)
        var startDate = NormalizeDateToUtcStart(request.StartDate);
        var endDate = NormalizeDateToUtcEnd(request.EndDate);

        // Validate view parameter first - hourly, daily, weekly, monthly allowed
        var validViews = new[] { "hourly", "daily", "weekly", "monthly" };
        var viewLower = request.View.ToLower();
        if (!validViews.Contains(viewLower))
            throw new ValidationException($"Vista inválida '{request.View}'. Valores permitidos: {string.Join(", ", validViews)}");

        // Validate date range - allow same day for any view
        if (startDate > endDate)
            throw new ValidationException("La fecha inicial no puede ser mayor a la fecha final.");

        // Calculate date range in days (comparing date parts only for clearer validation)
        var startDateOnly = startDate.Date;
        var endDateOnly = endDate.Date;
        var daysDifference = (endDateOnly - startDateOnly).Days;

        // Validate maximum date range based on view
        // For hourly view, we allow same day (0 days diff) up to 1 day difference
        var maxDaysAllowed = viewLower switch
        {
            "hourly" => 1,    // Max 1 day (can be same day or next day)
            "daily" => 14,    // Max 14 days
            "weekly" => 84,   // Max 12 weeks (84 days)
            "monthly" => 365, // Max 12 months (365 days)
            _ => 14
        };

        if (daysDifference > maxDaysAllowed)
        {
            var viewLabel = viewLower switch
            {
                "hourly" => "horaria (máximo 1 día)",
                "daily" => "diaria (máximo 14 días)",
                "weekly" => "semanal (máximo 84 días)",
                "monthly" => "mensual (máximo 365 días)",
                _ => $"{viewLower} (máximo {maxDaysAllowed} días)"
            };
            throw new ValidationException($"Rango de fechas inválido para vista {viewLabel}. Días seleccionados: {daysDifference}");
        }

        // Get aggregated data from repository using normalized dates
        var aggregatedData = await _repo.GetEnvironmentalAggregatesAsync(
            startDate,
            endDate,
            request.View
        );

        // Transform to response format with summary, trend, and variability
        var response = new List<EnvironmentalAggregateResponse>();

        foreach (var kvp in aggregatedData)
        {
            var variableCode = kvp.Key;
            var aggregates = kvp.Value;

            if (!aggregates.Any())
                continue;

            // Lookup variable to get the display name
            var variable = await _variableRepo.GetByCodeAsync(variableCode);
            var variableName = variable?.Name ?? variableCode; // Fallback to code if not found

            // Calculate summary (overall statistics) with rounding
            var summary = new AggregateSummary
            {
                Min = RoundToDecimals(aggregates.Min(a => a.Min), 2),
                Max = RoundToDecimals(aggregates.Max(a => a.Max), 2),
                Avg = RoundToDecimals(aggregates.Average(a => a.Avg), 2),
                Count = aggregates.Sum(a => a.Count)
            };

            // Calculate trend (avg per time period) with rounding
            List<AggregateTrendPoint> trend;
            var viewMode = request.View.ToLower();

            if (viewMode == "daily")
            {
                // For daily view, group hourly data by day and calculate daily average
                trend = aggregates
                    .GroupBy(a => a.Timestamp.Date)
                    .OrderBy(g => g.Key)
                    .Select(g => new AggregateTrendPoint
                    {
                        Timestamp = new DateTime(g.Key.Year, g.Key.Month, g.Key.Day, 0, 0, 0, DateTimeKind.Utc),
                        Avg = RoundToDecimals(g.Average(a => a.Avg), 2)
                    })
                    .ToList();
            }
            else
            {
                // For other views (hourly, weekly, monthly), use data as-is
                trend = aggregates
                    .OrderBy(a => a.Timestamp)
                    .Select(a => new AggregateTrendPoint
                    {
                        Timestamp = a.Timestamp,
                        Avg = RoundToDecimals(a.Avg, 2)
                    })
                    .ToList();
            }

            // Calculate variability ONLY for daily view
            List<AggregateVariabilityPoint>? variability = null;
            if (viewMode == "daily")
            {
                // For daily view, group hourly data by day to show intra-day variability (boxplot)
                variability = CalculateDailyVariability(aggregates);
            }

            response.Add(new EnvironmentalAggregateResponse
            {
                VariableCode = variableCode,
                VariableName = variableName,
                Summary = summary,
                Trend = trend,
                Variability = variability
            });
        }

        // Wrap response to match frontend expected structure
        return new EnvironmentalAggregatesResponse
        {
            Variables = response
        };
    }

    private List<AggregateVariabilityPoint> CalculateDailyVariability(List<Aggregate> aggregates)
    {
        // Group by day to calculate boxplot statistics
        var dailyGroups = aggregates
            .GroupBy(a => a.Timestamp.Date)
            .OrderBy(g => g.Key);

        var variability = new List<AggregateVariabilityPoint>();

        foreach (var dayGroup in dailyGroups)
        {
            var values = dayGroup.Select(a => a.Avg).OrderBy(v => v).ToList();
            var count = values.Count;

            if (count == 0)
                continue;

            // Calculate quartiles
            var min = values.First();
            var max = values.Last();
            var median = CalculatePercentile(values, 50);
            var q1 = CalculatePercentile(values, 25);
            var q3 = CalculatePercentile(values, 75);

            variability.Add(new AggregateVariabilityPoint
            {
                Timestamp = dayGroup.Key,
                Min = RoundToDecimals(min, 2),
                Q1 = RoundToDecimals(q1, 2),
                Median = RoundToDecimals(median, 2),
                Q3 = RoundToDecimals(q3, 2),
                Max = RoundToDecimals(max, 2),
                Count = count
            });
        }

        return variability;
    }

    private double CalculatePercentile(List<double> sortedValues, int percentile)
    {
        if (sortedValues.Count == 0)
            return 0;

        if (sortedValues.Count == 1)
            return sortedValues[0];

        var n = sortedValues.Count;
        var position = (percentile / 100.0) * (n - 1);
        var lower = (int)Math.Floor(position);
        var upper = (int)Math.Ceiling(position);

        if (lower == upper)
            return sortedValues[lower];

        var fraction = position - lower;
        return sortedValues[lower] + fraction * (sortedValues[upper] - sortedValues[lower]);
    }

    /// <summary>
    /// Rounds a double value to the specified number of decimal places
    /// </summary>
    private double RoundToDecimals(double value, int decimals)
    {
        return Math.Round(value, decimals);
    }

    /// <summary>
    /// Normalizes a date to the start of day in UTC (00:00:00.000)
    /// Handles timezone offsets from frontend by extracting date components
    /// </summary>
    private DateTime NormalizeDateToUtcStart(DateTime date)
    {
        // Extract date components and create UTC midnight
        return new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Utc);
    }

    /// <summary>
    /// Normalizes a date to the end of day in UTC (23:59:59.999)
    /// Handles timezone offsets from frontend by extracting date components
    /// </summary>
    private DateTime NormalizeDateToUtcEnd(DateTime date)
    {
        // Extract date components and create UTC end of day
        return new DateTime(date.Year, date.Month, date.Day, 23, 59, 59, 999, DateTimeKind.Utc);
    }

}
