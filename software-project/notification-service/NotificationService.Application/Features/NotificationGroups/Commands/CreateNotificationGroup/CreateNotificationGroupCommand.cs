using MediatR;
using NotificationService.Application.DTOs;

namespace NotificationService.Application.Features.NotificationGroups.Commands.CreateNotificationGroup;

/// <summary>
/// Represents a command to create a new notification group. This command encapsulates the necessary information required to create a notification group, including the group name, an optional description, and a list of recipients that belong to the group. When executed, this command will result in the creation of a new notification group with the specified details and return a DTO representing the created group.
/// </summary>
/// <param name="GroupName">The name of the notification group to be created.</param>
/// <param name="Description">An optional description of the notification group.</param>
/// <param name="Recipients">The list of recipients that belong to the notification group.</param>
public record CreateNotificationGroupCommand(
    string GroupName,
    string? Description,
    List<GroupRecipientDto> Recipients
) : IRequest<NotificationGroupDto>;
