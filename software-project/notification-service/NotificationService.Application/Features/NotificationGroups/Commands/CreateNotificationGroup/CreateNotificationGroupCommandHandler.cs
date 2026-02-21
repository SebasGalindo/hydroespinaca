using AutoMapper;
using MediatR;
using NotificationService.Application.DTOs;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces;

namespace NotificationService.Application.Features.NotificationGroups.Commands.CreateNotificationGroup;

/// <summary>
/// Handles the creation of a new notification group. This command handler processes the CreateNotificationGroupCommand, validates the input, checks for existing groups with the same name, and if valid, creates a new notification group in the repository. It then maps the created entity to a DTO and returns it as a response. If a group with the same name already exists, it throws an exception to prevent duplicates.
/// </summary>
public class CreateNotificationGroupCommandHandler
    : IRequestHandler<CreateNotificationGroupCommand, NotificationGroupDto>
{
    private readonly INotificationGroupRepository _repository;
    private readonly IMapper _mapper;

    public CreateNotificationGroupCommandHandler(
        INotificationGroupRepository repository,
        IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    /// <summary>
    /// Handles the CreateNotificationGroupCommand by validating the input, checking for existing groups, creating a new notification group if valid, and returning a DTO of the created group. Throws an exception if a group with the same name already exists to ensure uniqueness of notification groups.
    /// </summary>
    /// <param name="command">The command containing the notification group details to be created.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A DTO representing the newly created notification group.</returns>
    /// <exception cref="ArgumentException">Thrown when a notification group with the same name already exists.</exception>
    public async Task<NotificationGroupDto> Handle(
        CreateNotificationGroupCommand command,
        CancellationToken cancellationToken)
    {
        if (await _repository.ExistsAsync(command.GroupName, cancellationToken))
            throw new ArgumentException($"Notification group '{command.GroupName}' already exists.");
        
        // Create a new NotificationGroup entity based on the command data
        var entity = new NotificationGroup
        {
            GroupName = command.GroupName,
            Description = command.Description,
            Recipients = command.Recipients.Select(r => new GroupRecipient
            {
                Email = r.Email,
                Type = Enum.Parse<RecipientType>(r.Type.ToString()),
                IsActive = r.IsActive
            }).ToList()
        };

        // Save the new entity to the repository and return the created group as a DTO
        var created = await _repository.CreateAsync(entity, cancellationToken);
        return _mapper.Map<NotificationGroupDto>(created);
    }
}
