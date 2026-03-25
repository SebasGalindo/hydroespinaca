using BiService.Application.DTOs.Production;
using BiService.Application.Features.Production.Commands.CreateProductionRecord;
using BiService.Application.Features.Production.Commands.DeleteProductionRecord;
using BiService.Application.Features.Production.Queries.GetProductionRecordById;
using BiService.Application.Features.Production.Queries.GetProductionRecords;
using HydroEspinaca.Shared.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BiService.Api.Controllers;

/// <summary>
/// Controlador de API para la gestión de registros de producción de cultivos.
/// </summary>
[ApiController]
[Route("api/bi/production-records")]
public class ProductionRecordsController : ControllerBase
{
    private readonly ISender _sender;

    public ProductionRecordsController(ISender sender) => _sender = sender;

    /// <summary>
    /// Crea un nuevo registro de producción de cultivo.
    /// </summary>
    /// <param name="request">Datos del registro de producción a crear.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>El registro de producción creado con código 201.</returns>
    [HttpPost]
    [Authorize(Policy = PolicyNames.BiWrite)]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductionRecordRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateProductionRecordCommand(
            request.CropName,
            request.StartDate,
            request.HarvestDate,
            request.KilosProduced,
            request.PricePerKilo,
            request.Currency,
            request.Note,
            GetUserId());

        var created = await _sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>
    /// Obtiene todos los registros de producción.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Lista de todos los registros de producción.</returns>
    [HttpGet]
    [Authorize(Policy = PolicyNames.BiRead)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var records = await _sender.Send(new GetProductionRecordsQuery(), cancellationToken);
        return Ok(records);
    }

    /// <summary>
    /// Obtiene un registro de producción por su identificador.
    /// </summary>
    /// <param name="id">Identificador del registro de producción.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>El registro de producción encontrado o NotFound si no existe.</returns>
    [HttpGet("{id}")]
    [Authorize(Policy = PolicyNames.BiRead)]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        var record = await _sender.Send(new GetProductionRecordByIdQuery(id), cancellationToken);
        return record is null
            ? NotFound(new { message = $"No se encontró el registro de producción con ID '{id}'" })
            : Ok(record);
    }

    /// <summary>
    /// Elimina un registro de producción por su identificador.
    /// </summary>
    /// <param name="id">Identificador del registro de producción a eliminar.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>NoContent si la eliminación fue exitosa.</returns>
    [HttpDelete("{id}")]
    [Authorize(Policy = PolicyNames.BiWrite)]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteProductionRecordCommand(id), cancellationToken);
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
