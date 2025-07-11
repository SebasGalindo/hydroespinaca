using FluentValidation;
using SensorService.Application.DTOs;

namespace SensorService.Application.Validators;

public class SensorUpdateValidator : AbstractValidator<SensorUpdateDto>
{
    public SensorUpdateValidator()
    {
        RuleFor(x => x.PhysicalId).NotEmpty();
        RuleFor(x => x.Location).NotEmpty();
        RuleFor(x => x.Esp32Id).NotEmpty();
        RuleFor(x => x.SamplingFrequency).GreaterThan(0);
        RuleFor(x => x.Variables).NotEmpty();
        RuleFor(x => x.Status).Must(status => new[] { "Active", "Inactive", "Disconnected" }.Contains(status))
                               .WithMessage("Status must be one of: Active, Inactive, Disconnected");
    }
}
