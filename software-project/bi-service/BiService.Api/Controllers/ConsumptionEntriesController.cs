using BiService.Application.DTOs.Consumption;
using BiService.Application.Features.Consumption.Commands.CreateConsumptionEntry;
using BiService.Application.Features.Consumption.Commands.DeleteConsumptionEntry;
using BiService.Application.Features.Consumption.Queries.GetConsumptionEntries;
using BiService.Application.Features.Consumption.Queries.GetConsumptionSummary;
using BiService.Domain.Enums;
using HydroEspinaca.Shared.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BiService.Api.Controllers;

/// <summary>
/// Controlador de API para la gestión de registros de consumo manual de recursos.
/// </summary>
[ApiController]
[Route("api/bi/consumption-entries")]
public class ConsumptionEntriesController : ControllerBase
{
    private readonly ISender _sender;

    public ConsumptionEntriesController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Crea un nuevo registro de consumo manual.
    /// </summary>
    /// <param name="request">Datos del registro de consumo a crear.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>El registro de consumo creado con código 201.</returns>
    [HttpPost]
    [Authorize(Policy = PolicyNames.BiWrite)]
    public async Task<IActionResult> Create(
        [FromBody] CreateManualConsumptionEntryRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateConsumptionEntryCommand(
            request.Date,
            request.Type,
            request.Amount,
            request.Note,
            GetUserId());

        var created = await _sender.Send(command, cancellationToken);
        return CreatedAtAction(
            nameof(GetByRange),
            new { from = created.Date.Date, to = created.Date.Date },
            created);
    }

    /// <summary>
    /// Obtiene registros de consumo filtrados por rango de fechas y tipo opcional.
    /// </summary>
    /// <param name="from">Fecha de inicio del rango.</param>
    /// <param name="to">Fecha de fin del rango.</param>
    /// <param name="type">Tipo de consumo para filtrar (opcional).</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Lista de registros de consumo en el rango especificado.</returns>
    [HttpGet]
    [Authorize(Policy = PolicyNames.BiRead)]
    public async Task<IActionResult> GetByRange(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] ConsumptionType? type,
        CancellationToken cancellationToken)
    {
        var items = await _sender.Send(
            new GetConsumptionEntriesQuery(from, to, type), cancellationToken);
        return Ok(items);
    }

    /// <summary>
    /// Obtiene un resumen consolidado de consumo y costos para un periodo.
    /// </summary>
    /// <param name="from">Fecha de inicio del periodo.</param>
    /// <param name="to">Fecha de fin del periodo.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Resumen consolidado de consumo y costos.</returns>
    [HttpGet("summary")]
    [Authorize(Policy = PolicyNames.BiRead)]
    public async Task<IActionResult> GetSummary(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        CancellationToken cancellationToken)
    {
        var summary = await _sender.Send(
            new GetConsumptionSummaryQuery(from, to), cancellationToken);
        return Ok(summary);
    }

    /// <summary>
    /// Elimina un registro de consumo por su identificador.
    /// </summary>
    /// <param name="id">Identificador del registro de consumo a eliminar.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>NoContent si la eliminación fue exitosa.</returns>
    [HttpDelete("{id}")]
    [Authorize(Policy = PolicyNames.BiWrite)]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteConsumptionEntryCommand(id), cancellationToken);
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
