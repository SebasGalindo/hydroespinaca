using FluentValidation;
using HydroEspinaca.Shared.DTOs.Sensors;
using HydroEspinaca.Shared.Enums;
using SensorService.Domain.Interfaces;

namespace SensorService.Application.Validators.Sensor;

public class SensorUpdateValidator : AbstractValidator<SensorUpdateDto>
{
    public SensorUpdateValidator(
        IVariableRepository variableRepo,
        IEsp32NodeRepository esp32Repo)
    {
        {
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

            RuleFor(x => x.Status)
                 .IsInEnum()
                    .WithMessage($"El estado debe ser uno de los siguientes: {string.Join(", ", Enum.GetNames(typeof(SensorStatus)))}");

        }
    }
}
