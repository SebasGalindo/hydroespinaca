using FluentValidation;
using SensorService.Application.DTOs.Sensor;
using SensorService.Domain.Interfaces;

namespace SensorService.Application.Validators.Sensor;

public class SensorCreateValidator : AbstractValidator<SensorCreateDto>
{
    public SensorCreateValidator(
        IVariableRepository variableRepo,
        IEsp32NodeRepository esp32Repo)
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("El código es obligatorio.")
            .MaximumLength(50).WithMessage("El código no puede superar los 50 caracteres.");

        RuleFor(x => x.PhysicalId)
            .NotEmpty().WithMessage("El identificador físico es obligatorio.");

        RuleFor(x => x.Location)
            .NotEmpty().WithMessage("La ubicación es obligatoria.");

        RuleFor(x => x.Esp32Id)
            .NotEmpty().WithMessage("Debe especificarse un ESP32 válido.")
            .MustAsync(async (id, _) =>
            {
                var exists = await esp32Repo.ExistsAsync(id);
                return exists;
            }).WithMessage("El ESP32 especificado no existe.");

        RuleFor(x => x.SamplingFrequency)
            .GreaterThan(0).WithMessage("La frecuencia de muestreo debe ser mayor que cero.");

        RuleFor(x => x.Variables)
            .NotEmpty().WithMessage("Debe asociarse al menos una variable.")
            .MustAsync(async (variables, _) =>
            {
                var count = await variableRepo.CountByIdsAsync(variables);
                return count == variables.Count;
            }).WithMessage("Una o más variables no existen.");
    }
}
