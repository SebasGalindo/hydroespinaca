using HydroEspinaca.Shared.DTOs.Notifications;
using MediatR;

namespace NotificationService.Application.Features.Email.Commands.SendEmail;

/// <summary>
/// Represents a command to send an email notification. This command encapsulates the necessary information required to send an email, including the email content and an optional idempotency key to prevent duplicate sends.
/// </summary>
/// <param name="Request">The details of the email to be sent, including recipient, subject, and body.</param>
/// <param name="IdempotencyKey">An optional key to ensure that the same email is not sent multiple times in case of retries or duplicate requests.</param>
/// <returns>A response containing the status of the email sending operation, such as whether it was queued or sent successfully.</returns>
public record SendEmailCommand(
    SendEmailRequestDto Request,
    string? IdempotencyKey
) : IRequest<SendEmailResponseDto>;
