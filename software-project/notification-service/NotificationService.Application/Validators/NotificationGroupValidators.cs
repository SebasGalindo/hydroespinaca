using FluentValidation;
using NotificationService.Application.DTOs;

namespace NotificationService.Application.Validators;

public class CreateNotificationGroupValidator : AbstractValidator<CreateNotificationGroupDto>
{
    public CreateNotificationGroupValidator()
    {
        RuleFor(x => x.GroupName)
            .NotEmpty()
            .WithMessage("Group name is required")
            .Length(1, 100)
            .WithMessage("Group name must be between 1 and 100 characters")
            .Matches(@"^[a-zA-Z0-9\-_\s]+$")
            .WithMessage("Group name can only contain letters, numbers, hyphens, underscores and spaces");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("Description cannot exceed 500 characters");

        RuleFor(x => x.Recipients)
            .NotEmpty()
            .WithMessage("At least one recipient is required")
            .Must(recipients => recipients.Any(r => r.IsActive))
            .WithMessage("At least one recipient must be active");

        RuleForEach(x => x.Recipients)
            .SetValidator(new GroupRecipientValidator());
    }
}

public class UpdateNotificationGroupValidator : AbstractValidator<UpdateNotificationGroupDto>
{
    public UpdateNotificationGroupValidator()
    {
        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("Description cannot exceed 500 characters")
            .When(x => x.Description != null);

        RuleFor(x => x.Recipients)
            .Must(recipients => recipients!.Any(r => r.IsActive))
            .WithMessage("At least one recipient must be active")
            .When(x => x.Recipients != null && x.Recipients.Any());

        RuleForEach(x => x.Recipients)
            .SetValidator(new GroupRecipientValidator())
            .When(x => x.Recipients != null);
    }
}

public class GroupRecipientValidator : AbstractValidator<GroupRecipientDto>
{
    public GroupRecipientValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required")
            .EmailAddress()
            .WithMessage("Email must be a valid email address");

        RuleFor(x => x.Type)
            .IsInEnum()
            .WithMessage("Invalid recipient type");
    }
}