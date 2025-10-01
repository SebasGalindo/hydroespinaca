using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AuthService.Application.Features.Authentication.Commands.ForgotPassword;

/// <summary>
/// Handler for forgot password command
/// </summary>
public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, ForgotPasswordResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;
    private readonly ICodeGenerator _codeGenerator;
    private readonly IEmailTemplateRenderer _templateRenderer;
    private readonly INotificationService _notificationService;
    private readonly ILogger<ForgotPasswordCommandHandler> _logger;

    public ForgotPasswordCommandHandler(
        IUserRepository userRepository,
        IPasswordResetTokenRepository passwordResetTokenRepository,
        ICodeGenerator codeGenerator,
        IEmailTemplateRenderer templateRenderer,
        INotificationService notificationService,
        ILogger<ForgotPasswordCommandHandler> logger)
    {
        _userRepository = userRepository;
        _passwordResetTokenRepository = passwordResetTokenRepository;
        _codeGenerator = codeGenerator;
        _templateRenderer = templateRenderer;
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Handles the forgot password command
    /// </summary>
    /// <param name="request">Forgot password command</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Forgot password result</returns>
    public async Task<ForgotPasswordResult> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing forgot password request for email {Email}", request.Email);

            // Always return success to avoid email enumeration attacks
            // But only send email if user exists
            var user = await _userRepository.FindByEmailAsync(request.Email);
            if (user == null)
            {
                _logger.LogInformation("Forgot password requested for non-existent email {Email}", request.Email);
                return ForgotPasswordResult.Successful();
            }

            // Revoke any existing active tokens for this user
            await _passwordResetTokenRepository.RevokeActiveTokensForUserAsync(user.Id);

            // Generate new reset code
            var resetCode = _codeGenerator.GenerateResetCode();
            var expiresAt = DateTime.UtcNow.AddMinutes(15); // 15 minutes expiry

            // Create new password reset token
            var resetToken = new PasswordResetToken(user.Id, resetCode, expiresAt);
            await _passwordResetTokenRepository.CreateAsync(resetToken);

            // Render email template
            var emailModel = new
            {
                email = request.Email,
                code = resetCode,
                expiresAt = expiresAt.ToString("yyyy-MM-dd HH:mm:ss") + " UTC"
            };

            var htmlBody = await _templateRenderer.RenderTemplateAsync("reset-password", emailModel);

            // Send email
            var emailSent = await _notificationService.SendEmailAsync(
                request.Email,
                "Restablecimiento de contraseña - HydroEspinaca",
                htmlBody);

            if (!emailSent)
            {
                _logger.LogError("Failed to send password reset email to {Email}", request.Email);
                // Don't reveal email sending failure to prevent email enumeration
            }
            else
            {
                _logger.LogInformation("Password reset email sent successfully to {Email}", request.Email);
            }

            return ForgotPasswordResult.Successful();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing forgot password request for email {Email}", request.Email);
            return ForgotPasswordResult.Failed("An error occurred while processing your request");
        }
    }
}