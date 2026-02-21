using MediatR;
using NotificationService.Application.DTOs;

namespace NotificationService.Application.Features.NotificationGroups.Queries.GetAllNotificationGroups;

/// <summary>
/// Represents a query to retrieve all notification groups.
/// This query does not require any parameters and, when executed, 
/// will return a collection of DTOs representing all the notification groups available in the system.
/// Each DTO will contain the details of a notification group, such as its name, description, and list of recipients. 
/// </summary>
public record GetAllNotificationGroupsQuery() : IRequest<IEnumerable<NotificationGroupDto>>;
