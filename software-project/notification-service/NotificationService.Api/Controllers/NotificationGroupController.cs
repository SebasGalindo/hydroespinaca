using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Application.DTOs;
using NotificationService.Application.Features.NotificationGroups.Commands.CreateNotificationGroup;
using NotificationService.Application.Features.NotificationGroups.Commands.UpdateNotificationGroup;
using NotificationService.Application.Features.NotificationGroups.Commands.DeleteNotificationGroup;
using NotificationService.Application.Features.NotificationGroups.Queries.GetAllNotificationGroups;
using NotificationService.Application.Features.NotificationGroups.Queries.GetNotificationGroupByName;
using HydroEspinaca.Shared.Extensions;
using MediatR;

namespace NotificationService.Api.Controllers;

/// <summary>
/// Controller responsible for managing notification groups, 
/// including creating, updating, retrieving, and deleting groups.
/// </summary>
[ApiController]
[Route("api/notification-groups")]
public class NotificationGroupController : ControllerBase
{
    private readonly IMediator _mediator;

    public NotificationGroupController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Retrieves all notification groups. Requires notification:read scope.
    /// </summary>
    /// <param name="ct">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A collection of notification group DTOs.</returns>
    [HttpGet]
    [Authorize(Policy = PolicyNames.NotificationRead)]
    public async Task<ActionResult<IEnumerable<NotificationGroupDto>>> GetAllGroups(CancellationToken ct = default)
    {
        var query = new GetAllNotificationGroupsQuery();
        var result = await _mediator.Send(query, ct);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves a notification group by its name. Requires notification:read scope.
    /// </summary>
    /// <param name="groupName">The name of the notification group to retrieve.</param>
    /// <param name="ct">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The DTO of the notification group if found, otherwise a 404 Not Found response.</returns>
    [HttpGet("{groupName}")]
    [Authorize(Policy = PolicyNames.NotificationRead)]
    public async Task<ActionResult<NotificationGroupDto>> GetGroup(string groupName, CancellationToken ct = default)
    {
        var query = new GetNotificationGroupByNameQuery(groupName);
        var result = await _mediator.Send(query, ct);

        if (result == null)
            return NotFound($"Group '{groupName}' not found");

        return Ok(result);
    }

    /// <summary>
    /// Creates a new notification group. Requires notification:manage scope.
    /// </summary>
    /// <param name="dto">The details of the notification group to be created, including name, description, and recipients.</param>
    /// <param name="ct">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The DTO of the created notification group along with a 201 Created response.</returns>
    [HttpPost]
    [Authorize(Policy = PolicyNames.NotificationManage)]
    public async Task<ActionResult<NotificationGroupDto>> CreateGroup(
        [FromBody] CreateNotificationGroupDto dto,
        CancellationToken ct = default)
    {
        var command = new CreateNotificationGroupCommand(
            dto.GroupName,
            dto.Description,
            dto.Recipients);

        var result = await _mediator.Send(command, ct);

        return CreatedAtAction(
            nameof(GetGroup),
            new { groupName = result.GroupName },
            result);
    }

    /// <summary>
    /// Updates an existing notification group. Requires notification:manage scope.
    /// </summary>
    /// <param name="groupName">The name of the notification group to update.</param>
    /// <param name="dto">The updated details of the notification group, including description and recipients.</param>
    /// <param name="ct">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The DTO of the updated notification group.</returns>
    [HttpPut("{groupName}")]
    [Authorize(Policy = PolicyNames.NotificationManage)]
    public async Task<ActionResult<NotificationGroupDto>> UpdateGroup(
        string groupName,
        [FromBody] UpdateNotificationGroupDto dto,
        CancellationToken ct = default)
    {
        var command = new UpdateNotificationGroupCommand(
            groupName,
            dto.Description,
            dto.Recipients);

        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }

    /// <summary>
    /// Deletes a notification group. Requires notification:manage scope.
    /// </summary>
    /// <param name="groupName">The name of the notification group to delete.</param>
    /// <param name="ct">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A 204 No Content response if deletion is successful, otherwise a 404 Not Found response if the group does not exist.</returns>
    [HttpDelete("{groupName}")]
    [Authorize(Policy = PolicyNames.NotificationManage)]
    public async Task<ActionResult> DeleteGroup(string groupName, CancellationToken ct = default)
    {
        var command = new DeleteNotificationGroupCommand(groupName);
        await _mediator.Send(command, ct);
        return NoContent();
    }
}
