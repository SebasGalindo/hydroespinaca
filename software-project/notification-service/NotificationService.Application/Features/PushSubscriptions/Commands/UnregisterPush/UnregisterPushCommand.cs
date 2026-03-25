using MediatR;

namespace NotificationService.Application.Features.PushSubscriptions.Commands.UnregisterPush;

public record UnregisterPushCommand(string SubscriptionId) : IRequest<bool>;
