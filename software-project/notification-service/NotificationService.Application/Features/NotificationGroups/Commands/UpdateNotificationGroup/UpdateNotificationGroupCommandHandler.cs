using AutoMapper;
using MediatR;
using NotificationService.Application.DTOs;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces;

namespace NotificationService.Application.Features.NotificationGroups.Commands.UpdateNotificationGroup;

/// <summary>
/// Handles the update of an existing notification group. This command handler processes the UpdateNotificationGroupCommand, retrieves the existing group by its name, updates its properties based on the command data, and saves the changes to the repository. It then maps the updated entity to a DTO and returns it as a response. If the specified group does not exist, it throws an exception to indicate that the update operation cannot be performed on a non-existent group.
/// </summary>
public class UpdateNotificationGroupCommandHandler
    : IRequestHandler<UpdateNotificationGroupCommand, NotificationGroupDto>
{
    private readonly INotificationGroupRepository _repository;
    private readonly IMapper _mapper;

    public UpdateNotificationGroupCommandHandler(
        INotificationGroupRepository repository,
        IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    /// <summary>
    /// Handles the UpdateNotificationGroupCommand by retrieving the existing notification group, updating its properties based on the command data, saving the changes to the repository, and returning a DTO of the updated group. Throws an exception if the specified group does not exist to ensure that updates are only performed on valid groups.
    /// </summary>
    /// <param name="command">The command containing the updated notification group details.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A DTO representing the updated notification group.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the notification group with the specified name does not exist.</exception>
    public async Task<NotificationGroupDto> Handle(
        UpdateNotificationGroupCommand command,
        CancellationToken cancellationToken)
    {
        // Retrieve the existing notification group by its name
        var existing = await _repository.GetByGroupNameAsync(command.GroupName, cancellationToken)
            ?? throw new KeyNotFoundException($"Notification group '{command.GroupName}' not found.");

        if (command.Description != null)
            existing.Description = command.Description;

        if (command.Recipients != null)
        {
            existing.Recipients = command.Recipients.Select(r => new GroupRecipient
            {
                Email = r.Email,
                Type = Enum.Parse<RecipientType>(r.Type.ToString()),
                IsActive = r.IsActive
            }).ToList();
        }
        // Update the existing notification group in the repository and return the updated group as a DTO
        var updated = await _repository.UpdateAsync(command.GroupName, existing, cancellationToken)
            ?? throw new KeyNotFoundException($"Failed to update notification group '{command.GroupName}'.");

        return _mapper.Map<NotificationGroupDto>(updated);
    }
}
