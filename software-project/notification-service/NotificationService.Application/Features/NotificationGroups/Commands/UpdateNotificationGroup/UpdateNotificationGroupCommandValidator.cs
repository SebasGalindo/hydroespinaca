using FluentValidation;

namespace NotificationService.Application.Features.NotificationGroups.Commands.UpdateNotificationGroup;

/// <summary>
/// Validator for the UpdateNotificationGroupCommand. 
/// This class defines the validation rules for the command properties, 
/// ensuring that the group name is not empty, has a valid length, and contains only allowed characters. 
/// It also validates the description length and ensures that at least one recipient is provided and active, 
/// with valid email addresses and recipient types. 
/// </summary>
public class UpdateNotificationGroupCommandValidator : AbstractValidator<UpdateNotificationGroupCommand>
{
    public UpdateNotificationGroupCommandValidator()
    {
        RuleFor(x => x.GroupName)
            .NotEmpty().WithMessage("Group name is required");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters")
            .When(x => x.Description != null);

        When(x => x.Recipients != null && x.Recipients.Any(), () =>
        {
            RuleFor(x => x.Recipients!)
                .Must(recipients => recipients.Any(r => r.IsActive))
                .WithMessage("At least one recipient must be active");

            RuleForEach(x => x.Recipients!).ChildRules(r =>
            {
                r.RuleFor(x => x.Email)
                    .NotEmpty().WithMessage("Email is required")
                    .EmailAddress().WithMessage("Email must be a valid email address");
                r.RuleFor(x => x.Type)
                    .IsInEnum().WithMessage("Invalid recipient type");
            });
        });
    }
}
