using AutoMapper;
using MediatR;
using NotificationService.Application.DTOs;
using NotificationService.Domain.Interfaces;

namespace NotificationService.Application.Features.NotificationGroups.Queries.GetNotificationGroupByName;

/// <summary>
/// Handles the retrieval of a notification group by its name. 
/// This query handler processes the GetNotificationGroupByNameQuery,
/// retrieves the specified notification group from the repository, and maps it to a DTO.
/// </summary>
public class GetNotificationGroupByNameQueryHandler
    : IRequestHandler<GetNotificationGroupByNameQuery, NotificationGroupDto?>
{
    private readonly INotificationGroupRepository _repository;
    private readonly IMapper _mapper;

    public GetNotificationGroupByNameQueryHandler(
        INotificationGroupRepository repository,
        IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    /// <summary>
    /// Handles the GetNotificationGroupByNameQuery by retrieving the specified notification group
    /// from the repository and mapping it to a DTO.
    /// </summary>
    /// <param name="request">The query request containing the name of the notification group to retrieve.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The DTO of the notification group if found, otherwise null.</returns>
    public async Task<NotificationGroupDto?> Handle(
        GetNotificationGroupByNameQuery request,
        CancellationToken cancellationToken)
    {
        var group = await _repository.GetByGroupNameAsync(request.GroupName, cancellationToken);
        return group is null ? null : _mapper.Map<NotificationGroupDto>(group);
    }
}
