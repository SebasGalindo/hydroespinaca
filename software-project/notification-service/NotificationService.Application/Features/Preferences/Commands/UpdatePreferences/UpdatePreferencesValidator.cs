using FluentValidation;
using NotificationService.Domain.Entities;

namespace NotificationService.Application.Features.Preferences.Commands.UpdatePreferences;

public class UpdatePreferencesValidator : AbstractValidator<UpdatePreferencesCommand>
{
    public UpdatePreferencesValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();

        RuleForEach(x => x.Channels).ChildRules(channel =>
        {
            channel.RuleFor(c => c.Channel)
                .NotEmpty()
                .Must(c => NotificationChannels.All.Contains(c))
                .WithMessage("Invalid channel. Must be one of: " + string.Join(", ", NotificationChannels.All));
        });

        RuleFor(x => x.DailySummary).NotNull();
        RuleFor(x => x.DailySummary.Hour).InclusiveBetween(0, 23);
        RuleFor(x => x.DailySummary.Minute).InclusiveBetween(0, 59);

        When(x => x.QuietHours != null, () =>
        {
            RuleFor(x => x.QuietHours!.StartHour).InclusiveBetween(0, 23);
            RuleFor(x => x.QuietHours!.EndHour).InclusiveBetween(0, 23);
        });
    }
}
