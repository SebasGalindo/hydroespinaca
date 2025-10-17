using FluentValidation;
using HydroEspinaca.Shared.Errors;
using MongoDB.Bson;
using SensorService.Application.DTOs.Aggregate;
using SensorService.Application.Interfaces;
using SensorService.Application.Mappers;
using SensorService.Domain.Entities;
using SensorService.Domain.Interfaces;
using SensorService.Domain.Exceptions;

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

    public async Task<EnvironmentalAggregatesResponse> GetEnvironmentalAggregatesAsync(GetEnvironmentalAggregatesRequest request)
    {
        // Normalize dates to UTC midnight to handle timezone offsets from frontend
        // Frontend may send dates like "2025-10-09T05:00:00.000Z" (midnight in Colombia UTC-5)
        // We normalize to "2025-10-09T00:00:00.000Z" (midnight UTC)
        var startDate = NormalizeDateToUtcStart(request.StartDate);
        var endDate = NormalizeDateToUtcEnd(request.EndDate);

        // Validate date range
        if (startDate >= endDate)
            throw new ValidationException("La fecha inicial debe ser menor a la fecha final.");

        // Validate view parameter - only daily, weekly, monthly allowed
        var validViews = new[] { "daily", "weekly", "monthly" };
        if (!validViews.Contains(request.View.ToLower()))
            throw new ValidationException($"Vista inválida. Valores permitidos: {string.Join(", ", validViews)}");

        // Validate maximum date range based on view
        var dateRange = endDate - startDate;
        var maxRange = request.View.ToLower() switch
        {
            "daily" => TimeSpan.FromDays(14),    // Max 14 days
            "weekly" => TimeSpan.FromDays(84),   // Max 12 weeks
            "monthly" => TimeSpan.FromDays(365), // Max 12 months
            _ => TimeSpan.FromDays(14)
        };

        if (dateRange > maxRange)
        {
            var maxDays = (int)maxRange.TotalDays;
            throw new ValidationException($"Rango de fechas excede el límite permitido para la vista seleccionada (máximo: {maxDays} días)");
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

            // Calculate summary (overall statistics)
            var summary = new AggregateSummary
            {
                Min = aggregates.Min(a => a.Min),
                Max = aggregates.Max(a => a.Max),
                Avg = aggregates.Average(a => a.Avg),
                Count = aggregates.Sum(a => a.Count)
            };

            // Calculate trend (avg per time period)
            var trend = aggregates
                .OrderBy(a => a.Timestamp)
                .Select(a => new AggregateTrendPoint
                {
                    Timestamp = a.Timestamp,
                    Avg = a.Avg
                })
                .ToList();

            // Calculate variability (only for daily view)
            List<AggregateVariabilityPoint>? variability = null;
            if (request.View.ToLower() == "daily")
            {
                variability = CalculateDailyVariability(aggregates);
            }

            response.Add(new EnvironmentalAggregateResponse
            {
                VariableCode = variableCode,
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
                Min = min,
                Q1 = q1,
                Median = median,
                Q3 = q3,
                Max = max,
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
