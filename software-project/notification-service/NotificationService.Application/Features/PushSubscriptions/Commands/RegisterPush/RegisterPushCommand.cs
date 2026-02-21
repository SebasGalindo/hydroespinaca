using MediatR;
using NotificationService.Domain.Entities;

namespace NotificationService.Application.Features.PushSubscriptions.Commands.RegisterPush;

public record RegisterPushCommand(
    string UserId,
    string Platform,
    string Token,
    string? DeviceName
) : IRequest<PushSubscription>;
