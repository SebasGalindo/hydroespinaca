using MediatR;
using NotificationService.Application.DTOs;

namespace NotificationService.Application.Features.NotificationGroups.Commands.UpdateNotificationGroup;

/// <summary>
/// Represents a command to update an existing notification group. This command encapsulates the necessary information required to update a notification group, including the group name (which identifies the group to be updated), an optional description, and a list of recipients that belong to the group. When executed, this command will result in the update of the specified notification group with the new details and return a DTO representing the updated group.
/// </summary>
/// <param name="GroupName">The name of the notification group to be updated.</param>
/// <param name="Description">An optional description of the notification group.</param>
/// <param name="Recipients">The list of recipients that belong to the notification group.</param>
public record UpdateNotificationGroupCommand(
    string GroupName,
    string? Description,
    List<GroupRecipientDto>? Recipients
) : IRequest<NotificationGroupDto>;
