using FluentValidation;
using NotificationService.Application.DTOs;

namespace NotificationService.Application.Features.NotificationGroups.Commands.CreateNotificationGroup;

/// <summary>
/// Validator for the CreateNotificationGroupCommand. This class defines the validation rules for the command properties, ensuring that the group name is not empty, has a valid length, and contains only allowed characters. It also validates the description length and ensures that at least one recipient is provided and active, with valid email addresses and recipient types. This helps maintain data integrity and prevents invalid data from being processed when creating a new notification group.
/// </summary>
public class CreateNotificationGroupCommandValidator : AbstractValidator<CreateNotificationGroupCommand>
{
    public CreateNotificationGroupCommandValidator()
    {
        RuleFor(x => x.GroupName)
            .NotEmpty().WithMessage("Group name is required")
            .Length(1, 100).WithMessage("Group name must be between 1 and 100 characters")
            .Matches(@"^[a-zA-Z0-9\-_\s]+$")
            .WithMessage("Group name can only contain letters, numbers, hyphens, underscores and spaces");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters");

        RuleFor(x => x.Recipients)
            .NotEmpty().WithMessage("At least one recipient is required")
            .Must(recipients => recipients.Any(r => r.IsActive))
            .WithMessage("At least one recipient must be active");

        RuleForEach(x => x.Recipients).ChildRules(r =>
        {
            r.RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Email must be a valid email address");
            r.RuleFor(x => x.Type)
                .IsInEnum().WithMessage("Invalid recipient type");
        });
    }
}
