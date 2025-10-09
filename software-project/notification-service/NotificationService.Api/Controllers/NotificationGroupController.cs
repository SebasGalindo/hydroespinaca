using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Application.DTOs;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces;
using HydroEspinaca.Shared.Extensions;

namespace NotificationService.Api.Controllers;

[ApiController]
[Route("api/notification-groups")]
public class NotificationGroupController : ControllerBase
{
    private readonly INotificationGroupRepository _repository;

    public NotificationGroupController(INotificationGroupRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Obtiene todos los grupos de notificación
    /// </summary>
    [HttpGet]
    [Authorize(Policy = PolicyNames.NotificationRead)]
    public async Task<ActionResult<IEnumerable<NotificationGroupDto>>> GetAllGroups(CancellationToken ct = default)
    {
        var groups = await _repository.GetAllAsync(ct);
        var dtos = groups.Select(MapToDto);
        return Ok(dtos);
    }

    /// <summary>
    /// Obtiene un grupo por nombre
    /// </summary>
    [HttpGet("{groupName}")]
    [Authorize(Policy = PolicyNames.NotificationRead)]
    public async Task<ActionResult<NotificationGroupDto>> GetGroup(string groupName, CancellationToken ct = default)
    {
        var group = await _repository.GetByGroupNameAsync(groupName, ct);
        if (group == null)
        {
            return NotFound($"Group '{groupName}' not found");
        }

        return Ok(MapToDto(group));
    }

    /// <summary>
    /// Crea un nuevo grupo de notificación
    /// </summary>
    [HttpPost]
    [Authorize(Policy = PolicyNames.NotificationManage)]
    public async Task<ActionResult<NotificationGroupDto>> CreateGroup(
        [FromBody] CreateNotificationGroupDto dto, 
        CancellationToken ct = default)
    {
        // Verificar que el grupo no exista
        if (await _repository.ExistsAsync(dto.GroupName, ct))
        {
            return Conflict($"Group '{dto.GroupName}' already exists");
        }

        var group = MapToEntity(dto);
        var createdGroup = await _repository.CreateAsync(group, ct);
        var responseDto = MapToDto(createdGroup);

        return CreatedAtAction(
            nameof(GetGroup), 
            new { groupName = createdGroup.GroupName }, 
            responseDto);
    }

    /// <summary>
    /// Actualiza un grupo existente
    /// </summary>
    [HttpPut("{groupName}")]
    [Authorize(Policy = PolicyNames.NotificationManage)]
    public async Task<ActionResult<NotificationGroupDto>> UpdateGroup(
        string groupName, 
        [FromBody] UpdateNotificationGroupDto dto, 
        CancellationToken ct = default)
    {
        var existingGroup = await _repository.GetByGroupNameAsync(groupName, ct);
        if (existingGroup == null)
        {
            return NotFound($"Group '{groupName}' not found");
        }

        // Aplicar cambios
        if (dto.Description != null)
        {
            existingGroup.Description = dto.Description;
        }

        if (dto.Recipients != null)
        {
            existingGroup.Recipients = dto.Recipients.Select(MapRecipientToEntity).ToList();
        }

        var updatedGroup = await _repository.UpdateAsync(groupName, existingGroup, ct);
        if (updatedGroup == null)
        {
            return NotFound($"Group '{groupName}' not found");
        }

        return Ok(MapToDto(updatedGroup));
    }

    /// <summary>
    /// Elimina un grupo de notificación
    /// </summary>
    [HttpDelete("{groupName}")]
    [Authorize(Policy = PolicyNames.NotificationManage)]
    public async Task<ActionResult> DeleteGroup(string groupName, CancellationToken ct = default)
    {
        var deleted = await _repository.DeleteAsync(groupName, ct);
        if (!deleted)
        {
            return NotFound($"Group '{groupName}' not found");
        }

        return NoContent();
    }

    // Mapping methods
    private static NotificationGroupDto MapToDto(NotificationGroup group)
    {
        return new NotificationGroupDto
        {
            Id = group.Id,
            GroupName = group.GroupName,
            Description = group.Description,
            Recipients = group.Recipients.Select(MapRecipientToDto).ToList(),
            CreatedAt = group.CreatedAt,
            UpdatedAt = group.UpdatedAt
        };
    }

    private static GroupRecipientDto MapRecipientToDto(GroupRecipient recipient)
    {
        return new GroupRecipientDto
        {
            Email = recipient.Email,
            Type = (RecipientTypeDto)recipient.Type,
            IsActive = recipient.IsActive
        };
    }

    private static NotificationGroup MapToEntity(CreateNotificationGroupDto dto)
    {
        return new NotificationGroup
        {
            GroupName = dto.GroupName,
            Description = dto.Description,
            Recipients = dto.Recipients.Select(MapRecipientToEntity).ToList()
        };
    }

    private static GroupRecipient MapRecipientToEntity(GroupRecipientDto dto)
    {
        return new GroupRecipient
        {
            Email = dto.Email,
            Type = (RecipientType)dto.Type,
            IsActive = dto.IsActive
        };
    }
}