using MediatR;
using NotificationService.Domain.Entities;

namespace NotificationService.Application.Features.PushSubscriptions.Queries.GetSubscriptions;

public record GetSubscriptionsQuery(string UserId, string? Platform = null) : IRequest<IEnumerable<PushSubscription>>;
