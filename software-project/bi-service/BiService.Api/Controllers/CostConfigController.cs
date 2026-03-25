using BiService.Application.DTOs.CostConfig;
using BiService.Application.Features.CostConfig.Commands.CreateCostConfigVersion;
using BiService.Application.Features.CostConfig.Commands.UpdateCostConfigVersion;
using BiService.Application.Features.CostConfig.Commands.DeleteCostConfigVersion;
using BiService.Application.Features.CostConfig.Queries.GetCostConfigVersions;
using BiService.Application.Features.CostConfig.Queries.GetCurrentCostConfig;
using HydroEspinaca.Shared.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BiService.Api.Controllers;

/// <summary>
/// Controlador de API para la gestión de versiones de configuración de costos unitarios.
/// </summary>
[ApiController]
[Route("api/bi/cost-config")]
public class CostConfigController : ControllerBase
{
    private readonly ISender _sender;

    public CostConfigController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Obtiene la configuración de costos activa actualmente.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>La configuración de costos activa o NotFound si no existe ninguna.</returns>
    [HttpGet("current")]
    [Authorize(Policy = PolicyNames.BiRead)]
    public async Task<IActionResult> GetCurrent(CancellationToken cancellationToken)
    {
        var current = await _sender.Send(new GetCurrentCostConfigQuery(), cancellationToken);
        if (current is null)
        {
            return NotFound(new { message = "No hay configuración de costos activa" });
        }

        return Ok(current);
    }

    /// <summary>
    /// Lista las versiones de configuración de costos con filtro opcional por rango de fechas.
    /// </summary>
    /// <param name="from">Fecha de inicio del rango (opcional).</param>
    /// <param name="to">Fecha de fin del rango (opcional).</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Lista de versiones de configuración de costos.</returns>
    [HttpGet("versions")]
    [Authorize(Policy = PolicyNames.BiRead)]
    public async Task<IActionResult> GetVersions(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        var versions = await _sender.Send(
            new GetCostConfigVersionsQuery(from, to), cancellationToken);
        return Ok(versions);
    }

    /// <summary>
    /// Crea una nueva versión de configuración de costos, desactivando la anterior.
    /// </summary>
    /// <param name="request">Datos de la nueva versión de configuración de costos.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>La versión de configuración creada con código 201.</returns>
    [HttpPost("versions")]
    [Authorize(Policy = PolicyNames.BiWrite)]
    public async Task<IActionResult> CreateVersion(
        [FromBody] CreateCostConfigVersionRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateCostConfigVersionCommand(
            request.Currency,
            request.ElectricityCostPerKwh,
            request.WaterCostPerLiter,
            request.NutrientCostPerLiter,
            request.EffectiveFrom,
            request.EffectiveTo,
            GetUserId());

        var created = await _sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetCurrent), new { }, created);
    }

    /// <summary>
    /// Actualiza una versión de configuración de costos existente.
    /// </summary>
    [HttpPut("versions/{id}")]
    [Authorize(Policy = PolicyNames.BiWrite)]
    public async Task<IActionResult> UpdateVersion(
        string id,
        [FromBody] UpdateCostConfigVersionRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateCostConfigVersionCommand(
            id,
            request.Currency,
            request.ElectricityCostPerKwh,
            request.WaterCostPerLiter,
            request.NutrientCostPerLiter,
            request.EffectiveFrom,
            request.EffectiveTo,
            GetUserId());

        var updated = await _sender.Send(command, cancellationToken);
        return Ok(updated);
    }

    /// <summary>
    /// Elimina una versión de configuración de costos.
    /// </summary>
    [HttpDelete("versions/{id}")]
    [Authorize(Policy = PolicyNames.BiWrite)]
    public async Task<IActionResult> DeleteVersion(
        string id,
        CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteCostConfigVersionCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Extrae el identificador del usuario autenticado desde los claims del JWT.
    /// </summary>
    /// <returns>El identificador del usuario o "unknown" si no se puede determinar.</returns>
    private string GetUserId()
    {
        return User.FindFirst("sub")?.Value
            ?? User.FindFirst("client_id")?.Value
            ?? User.Identity?.Name
            ?? "unknown";
    }
}
