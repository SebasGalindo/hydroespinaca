using MediatR;
using Microsoft.Extensions.Logging;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces;

namespace NotificationService.Application.Features.PushSubscriptions.Commands.RegisterPush;

public class RegisterPushHandler : IRequestHandler<RegisterPushCommand, PushSubscription>
{
    private readonly IPushSubscriptionRepository _repository;
    private readonly ILogger<RegisterPushHandler> _logger;

    public RegisterPushHandler(IPushSubscriptionRepository repository, ILogger<RegisterPushHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<PushSubscription> Handle(RegisterPushCommand command, CancellationToken ct)
    {
        // Check if this token already exists for this user (avoid duplicates)
        var existing = await _repository.GetByUserIdAsync(command.UserId, ct);
        var duplicate = existing.FirstOrDefault(s => s.Token == command.Token);

        if (duplicate != null)
        {
            // Re-activate if it was deactivated
            if (!duplicate.IsActive)
            {
                duplicate.IsActive = true;
                duplicate.FailureCount = 0;
                duplicate.LastUsedAt = DateTime.UtcNow;
                duplicate.DeviceName = command.DeviceName ?? duplicate.DeviceName;
                await _repository.UpdateAsync(duplicate, ct);
                _logger.LogInformation("Reactivated push subscription {Id} for user {UserId}",
                    duplicate.Id, command.UserId);
            }
            return duplicate;
        }

        var subscription = new PushSubscription
        {
            UserId = command.UserId,
            Platform = command.Platform,
            Token = command.Token,
            DeviceName = command.DeviceName
        };

        await _repository.CreateAsync(subscription, ct);
        _logger.LogInformation("Registered new {Platform} push subscription for user {UserId}",
            command.Platform, command.UserId);

        return subscription;
    }
}
