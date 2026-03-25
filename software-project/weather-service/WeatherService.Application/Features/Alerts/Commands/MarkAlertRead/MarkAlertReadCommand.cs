using MediatR;

namespace WeatherService.Application.Features.Alerts.Commands.MarkAlertRead;

/// <summary>
/// Marks a weather alert as read for a specific user.
/// </summary>
public record MarkAlertReadCommand(string AlertId, string UserId) : IRequest<bool>;
