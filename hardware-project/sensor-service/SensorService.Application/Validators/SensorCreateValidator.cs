using FluentValidation;
using SensorService.Application.DTOs;

namespace SensorService.Application.Validators;

public class SensorCreateValidator : AbstractValidator<SensorCreateDto>
{
    public SensorCreateValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.PhysicalId).NotEmpty();
        RuleFor(x => x.Location).NotEmpty();
        RuleFor(x => x.Esp32Id).NotEmpty();
        RuleFor(x => x.SamplingFrequency).GreaterThan(0);
        RuleFor(x => x.Variables).NotEmpty();
    }
}