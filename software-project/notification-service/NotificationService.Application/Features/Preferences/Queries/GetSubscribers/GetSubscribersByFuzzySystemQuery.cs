using MediatR;
using NotificationService.Application.DTOs;

namespace NotificationService.Application.Features.Preferences.Queries.GetSubscribers;

/// <summary>
/// Gets all users subscribed to weather alerts for a specific fuzzy system.
/// Used by weather-service via GET /api/preferences/subscribers?fuzzySystemId=xxx.
/// </summary>
public record GetSubscribersByFuzzySystemQuery(string FuzzySystemId)
    : IRequest<IEnumerable<SubscriberDto>>;
