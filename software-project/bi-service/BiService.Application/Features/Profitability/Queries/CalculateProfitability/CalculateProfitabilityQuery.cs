using BiService.Application.DTOs.OperationalCost;
using BiService.Application.DTOs.Profitability;
using MediatR;

namespace BiService.Application.Features.Profitability.Queries.CalculateProfitability;

/// <summary>
/// Query para calcular la rentabilidad de un registro de producción.
/// </summary>
public record CalculateProfitabilityQuery(
    string ProductionRecordId,
    List<ActuatorDurationInput> ActuatorDurations,
    decimal? InitialInvestmentCost = null
) : IRequest<ProfitabilityResponse>;
