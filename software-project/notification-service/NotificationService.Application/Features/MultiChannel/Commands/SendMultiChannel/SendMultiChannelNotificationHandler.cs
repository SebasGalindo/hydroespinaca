using MediatR;
using Microsoft.Extensions.Logging;
using NotificationService.Application.DTOs;
using NotificationService.Domain.Interfaces;

namespace NotificationService.Application.Features.MultiChannel.Commands.SendMultiChannel;

/// <summary>
/// Handler for the SendMultiChannelNotificationCommand. 
/// This handler is responsible for processing the command to send a notification 
/// to a user via all their enabled channels.
/// </summary>
public class SendMultiChannelNotificationHandler
    : IRequestHandler<SendMultiChannelNotificationCommand, SendMultiChannelNotificationResponse>
{
    private readonly INotificationDispatcher _dispatcher;
    private readonly ILogger<SendMultiChannelNotificationHandler> _logger;

    public SendMultiChannelNotificationHandler(
        INotificationDispatcher dispatcher,
        ILogger<SendMultiChannelNotificationHandler> logger)
    {
        _dispatcher = dispatcher;
        _logger = logger;
    }

    public async Task<SendMultiChannelNotificationResponse> Handle(
        SendMultiChannelNotificationCommand command, CancellationToken ct)
    {
        _logger.LogInformation(
            "Sending multi-channel notification to user {UserId}, template: {TemplateKey}",
            command.UserId, command.TemplateKey);

        var results = await _dispatcher.DispatchAsync(
            command.UserId,
            command.TemplateKey,
            command.Title,
            command.Body,
            command.Data,
            null,
            ct);

        var channelResults = results
            .Select(r => new ChannelResult(r.Channel, r.Success, r.Provider, r.Error))
            .ToList();

        var successCount = channelResults.Count(r => r.Success);
        _logger.LogInformation(
            "Multi-channel send complete for user {UserId}: {SuccessCount}/{TotalCount} channels succeeded",
            command.UserId, successCount, channelResults.Count);

        return new SendMultiChannelNotificationResponse(
            Guid.NewGuid().ToString("N"),
            channelResults);
    }
}
