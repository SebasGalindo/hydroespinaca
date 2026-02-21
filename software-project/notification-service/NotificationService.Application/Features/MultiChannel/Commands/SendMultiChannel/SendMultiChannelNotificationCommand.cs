using MediatR;
using NotificationService.Application.DTOs;

namespace NotificationService.Application.Features.MultiChannel.Commands.SendMultiChannel;

/// <summary>
/// Command to send a notification to a user via all their enabled channels.
/// The dispatcher resolves preferences and sends accordingly.
/// </summary>
public record SendMultiChannelNotificationCommand(
    string UserId,
    string TemplateKey,
    string Title,
    string Body,
    Dictionary<string, string>? Data = null
) : IRequest<SendMultiChannelNotificationResponse>;
