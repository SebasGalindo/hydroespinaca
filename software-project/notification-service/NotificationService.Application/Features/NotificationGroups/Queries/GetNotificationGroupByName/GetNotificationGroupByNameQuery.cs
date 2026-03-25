using MediatR;
using NotificationService.Application.DTOs;

namespace NotificationService.Application.Features.NotificationGroups.Queries.GetNotificationGroupByName;

/// <summary>
/// Represents a query to retrieve a notification group by its name.
/// </summary>
/// <param name="GroupName">The name of the notification group to retrieve.</param>
public record GetNotificationGroupByNameQuery(string GroupName) : IRequest<NotificationGroupDto?>;
