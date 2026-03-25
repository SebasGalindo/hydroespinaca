using MediatR;

namespace NotificationService.Application.Features.NotificationGroups.Commands.DeleteNotificationGroup;

/// <summary>
/// Represents a command to delete an existing notification group. 
/// This command encapsulates the necessary information required to identify the notification group 
/// to be deleted, which is primarily the group name. 
/// When executed, this command will result in the deletion of the specified notification group 
/// from the repository. If the group does not exist, it will throw an exception to indicate that 
/// the deletion operation cannot be performed on a non-existent group.
/// </summary>
/// <param name="GroupName">The name of the notification group to be deleted.</param>
public record DeleteNotificationGroupCommand(string GroupName) : IRequest<bool>;
