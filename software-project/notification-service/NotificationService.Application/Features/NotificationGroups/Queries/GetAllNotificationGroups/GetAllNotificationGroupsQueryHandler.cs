using AutoMapper;
using MediatR;
using NotificationService.Application.DTOs;
using NotificationService.Domain.Interfaces;

namespace NotificationService.Application.Features.NotificationGroups.Queries.GetAllNotificationGroups;

/// <summary>
/// Handles the retrieval of all notification groups. 
/// This query handler processes the GetAllNotificationGroupsQuery,
/// retrieves all notification groups from the repository, and maps them to DTOs.
/// </summary>
public class GetAllNotificationGroupsQueryHandler
    : IRequestHandler<GetAllNotificationGroupsQuery, IEnumerable<NotificationGroupDto>>
{
    private readonly INotificationGroupRepository _repository;
    private readonly IMapper _mapper;

    public GetAllNotificationGroupsQueryHandler(
        INotificationGroupRepository repository,
        IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    /// <summary>
    /// Handles the GetAllNotificationGroupsQuery by retrieving all notification groups 
    /// from the repository and mapping them to a collection of DTOs.
    /// </summary>
    /// <param name="request">The query request containing no parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of notification group DTOs.</returns>
    public async Task<IEnumerable<NotificationGroupDto>> Handle(
        GetAllNotificationGroupsQuery request,
        CancellationToken cancellationToken)
    {
        var groups = await _repository.GetAllAsync(cancellationToken);
        return _mapper.Map<IEnumerable<NotificationGroupDto>>(groups);
    }
}
