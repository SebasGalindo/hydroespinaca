using MediatR;
using NotificationService.Domain.Entities;

namespace NotificationService.Application.Features.NotificationHistory.Queries.GetHistory;

public record GetNotificationHistoryQuery(
    string UserId,
    string? Channel = null,
    DateTime? From = null,
    DateTime? To = null,
    int Limit = 100
) : IRequest<IEnumerable<NotificationLog>>;
