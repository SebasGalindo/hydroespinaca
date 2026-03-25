using MediatR;
using NotificationService.Domain.Interfaces;

namespace NotificationService.Application.Features.PushSubscriptions.Commands.UnregisterPush;

public class UnregisterPushHandler : IRequestHandler<UnregisterPushCommand, bool>
{
    private readonly IPushSubscriptionRepository _repository;

    public UnregisterPushHandler(IPushSubscriptionRepository repository)
        => _repository = repository;

    public async Task<bool> Handle(UnregisterPushCommand command, CancellationToken ct)
        => await _repository.DeleteAsync(command.SubscriptionId, ct);
}
