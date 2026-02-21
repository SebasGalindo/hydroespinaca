using MediatR;
using NotificationService.Domain.Entities;

namespace NotificationService.Application.Features.Preferences.Queries.GetPreferences;

public record GetPreferencesQuery(string UserId) : IRequest<NotificationPreference?>;
