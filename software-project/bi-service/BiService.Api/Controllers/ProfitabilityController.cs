using BiService.Application.DTOs.Profitability;
using BiService.Application.Features.Profitability.Queries.CalculateProfitability;
using HydroEspinaca.Shared.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BiService.Api.Controllers;

/// <summary>
/// Controlador de API para el cálculo de rentabilidad de ciclos de producción.
/// </summary>
[ApiController]
[Route("api/bi/profitability")]
public class ProfitabilityController : ControllerBase
{
    private readonly ISender _sender;

    public ProfitabilityController(ISender sender) => _sender = sender;

    /// <summary>
    /// Calcula la rentabilidad de un registro de producción comparando ingresos contra costos operacionales y de consumo.
    /// </summary>
    /// <param name="request">Datos del cálculo incluyendo el identificador del registro de producción y las duraciones de actuadores.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>El resultado del cálculo de rentabilidad.</returns>
    [HttpPost("calculate")]
    [Authorize(Policy = PolicyNames.BiRead)]
    public async Task<IActionResult> Calculate(
        [FromBody] CalculateProfitabilityRequest request,
        CancellationToken cancellationToken)
    {
        var query = new CalculateProfitabilityQuery(
            request.ProductionRecordId,
            request.ActuatorDurations,
            request.InitialInvestmentCost);

        var result = await _sender.Send(query, cancellationToken);
        return Ok(result);
    }
}
