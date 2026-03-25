using MediatR;
using NotificationService.Domain.Interfaces;

namespace NotificationService.Application.Features.NotificationGroups.Commands.DeleteNotificationGroup;

/// <summary>
/// Handles the deletion of an existing notification group. 
/// This command handler processes the DeleteNotificationGroupCommand, 
/// attempts to delete the specified group from the repository, 
/// and returns a boolean indicating the success of the operation. I
/// f the specified group does not exist, it throws an exception to indicate that the deletion 
/// cannot be performed on a non-existent group.
/// </summary>
public class DeleteNotificationGroupCommandHandler
    : IRequestHandler<DeleteNotificationGroupCommand, bool>
{
    private readonly INotificationGroupRepository _repository;

    public DeleteNotificationGroupCommandHandler(INotificationGroupRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Handles the DeleteNotificationGroupCommand by attempting to delete the specified notification
    /// group from the repository. If the group does not exist, 
    /// </summary>
    /// <param name="command">The command containing the group name to be deleted.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the deletion was successful, otherwise false.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the notification group does not exist.</exception>
    public async Task<bool> Handle(
        DeleteNotificationGroupCommand command,
        CancellationToken cancellationToken)
    {
        var deleted = await _repository.DeleteAsync(command.GroupName, cancellationToken);
        if (!deleted)
            throw new KeyNotFoundException($"Notification group '{command.GroupName}' not found.");

        return true;
    }
}
