using FluentValidation;

namespace WeatherService.Application.Features.Alerts.Commands.UpdateAlertConfig;

/// <summary>
/// Validator for the UpdateAlertConfigCommand, ensuring that the command's properties are valid before processing.
/// </summary>
public class UpdateAlertConfigValidator : AbstractValidator<UpdateAlertConfigCommand>
{
    private static readonly HashSet<string> ValidTypes = new(
        Domain.Enums.AlertTypes.All, StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> ValidComparisons = new(
        ["gt", "lt"], StringComparer.OrdinalIgnoreCase);

    public UpdateAlertConfigValidator()
    {
        RuleFor(x => x.FuzzySystemId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleForEach(x => x.Alerts).ChildRules(alert =>
        {
            alert.RuleFor(a => a.Type)
                .NotEmpty()
                .Must(t => ValidTypes.Contains(t))
                .WithMessage("Invalid alert type: {PropertyValue}");

            alert.RuleFor(a => a.Comparison)
                .Must(c => string.IsNullOrEmpty(c) || ValidComparisons.Contains(c))
                .WithMessage("Comparison must be 'gt', 'lt' or empty");

            alert.RuleFor(a => a.Recommendation).NotNull();
        });
    }
}
