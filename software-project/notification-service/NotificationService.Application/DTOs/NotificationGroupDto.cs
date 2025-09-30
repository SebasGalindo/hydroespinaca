using NotificationService.Domain.Entities;

namespace NotificationService.Application.DTOs;

// DTO para respuesta de API con información completa del grupo
public class NotificationGroupDto
{
    public required string Id { get; init; }
    public required string GroupName { get; init; }
    public string? Description { get; init; }
    public List<GroupRecipientDto> Recipients { get; init; } = [];
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

// DTO para creación de grupo
public class CreateNotificationGroupDto
{
    public required string GroupName { get; init; }
    public string? Description { get; init; }
    public List<GroupRecipientDto> Recipients { get; init; } = [];
}

// DTO para actualización de grupo
public class UpdateNotificationGroupDto
{
    public string? Description { get; init; }
    public List<GroupRecipientDto>? Recipients { get; init; }
}

// DTO para destinatario del grupo
public class GroupRecipientDto
{
    public required string Email { get; init; }
    public RecipientTypeDto Type { get; init; } = RecipientTypeDto.TO;
    public bool IsActive { get; init; } = true;
}

// Enum para el tipo de destinatario en DTOs
public enum RecipientTypeDto
{
    TO,
    CC,
    BCC
}