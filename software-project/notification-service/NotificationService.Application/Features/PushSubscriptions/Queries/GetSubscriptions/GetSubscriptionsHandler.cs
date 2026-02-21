using MediatR;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces;

namespace NotificationService.Application.Features.PushSubscriptions.Queries.GetSubscriptions;

public class GetSubscriptionsHandler : IRequestHandler<GetSubscriptionsQuery, IEnumerable<PushSubscription>>
{
    private readonly IPushSubscriptionRepository _repository;

    public GetSubscriptionsHandler(IPushSubscriptionRepository repository)
        => _repository = repository;

    public async Task<IEnumerable<PushSubscription>> Handle(GetSubscriptionsQuery request, CancellationToken ct)
        => await _repository.GetActiveByUserIdAsync(request.UserId, request.Platform, ct);
}
