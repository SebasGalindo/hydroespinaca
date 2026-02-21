using MediatR;
using NotificationService.Application.DTOs;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces;

namespace NotificationService.Application.Features.Preferences.Queries.GetSubscribers;

public class GetSubscribersByFuzzySystemHandler
    : IRequestHandler<GetSubscribersByFuzzySystemQuery, IEnumerable<SubscriberDto>>
{
    private readonly INotificationPreferenceRepository _repository;

    public GetSubscribersByFuzzySystemHandler(INotificationPreferenceRepository repository)
        => _repository = repository;

    public async Task<IEnumerable<SubscriberDto>> Handle(
        GetSubscribersByFuzzySystemQuery request, CancellationToken ct)
    {
        var preferences = await _repository.GetSubscribersForFuzzySystemAsync(request.FuzzySystemId, ct);

        return preferences.Select(p => new SubscriberDto(
            p.UserId,
            p.GetEnabledChannels(),
            p.Channels.FirstOrDefault(c => c.Channel == NotificationChannels.Email)?.Target,
            p.Channels.FirstOrDefault(c => c.Channel == NotificationChannels.WhatsApp)?.Target
        ));
    }
}
