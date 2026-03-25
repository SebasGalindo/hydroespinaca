using MediatR;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces;

namespace NotificationService.Application.Features.Preferences.Queries.GetPreferences;

public class GetPreferencesHandler : IRequestHandler<GetPreferencesQuery, NotificationPreference?>
{
    private readonly INotificationPreferenceRepository _repository;

    public GetPreferencesHandler(INotificationPreferenceRepository repository)
        => _repository = repository;

    public async Task<NotificationPreference?> Handle(GetPreferencesQuery request, CancellationToken ct)
        => await _repository.GetByUserIdAsync(request.UserId, ct);
}
