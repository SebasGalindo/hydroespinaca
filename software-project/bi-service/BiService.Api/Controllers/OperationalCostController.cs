using BiService.Application.DTOs.OperationalCost;
using BiService.Application.Features.OperationalCost.Queries.CalculateOperationalCost;
using HydroEspinaca.Shared.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BiService.Api.Controllers;

/// <summary>
/// Controlador de API para el cálculo de costos operacionales por consumo eléctrico de actuadores.
/// </summary>
[ApiController]
[Route("api/bi/operational-cost")]
public class OperationalCostController : ControllerBase
{
    private readonly ISender _sender;

    public OperationalCostController(ISender sender) => _sender = sender;

    /// <summary>
    /// Calcula el costo operacional estimado para un conjunto de actuadores en un periodo dado.
    /// </summary>
    /// <param name="request">Datos del cálculo incluyendo periodo y duraciones de actuadores.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>El resultado del cálculo de costo operacional.</returns>
    [HttpPost("calculate")]
    [Authorize(Policy = PolicyNames.BiRead)]
    public async Task<IActionResult> Calculate(
        [FromBody] CalculateOperationalCostRequest request,
        CancellationToken cancellationToken)
    {
        var query = new CalculateOperationalCostQuery(
            request.From,
            request.To,
            request.ActuatorDurations);

        var result = await _sender.Send(query, cancellationToken);
        return Ok(result);
    }
}
