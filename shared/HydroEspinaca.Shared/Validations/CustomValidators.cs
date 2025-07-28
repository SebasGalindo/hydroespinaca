using FluentValidation;
namespace HydroEspinaca.Shared.Validations;

public static class CustomValidators
{
    public static IRuleBuilderOptions<T, string> BeValidObjectId<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty().WithMessage("El ID no puede ser vacío.")
            .Must(x => MongoDB.Bson.ObjectId.TryParse(x, out _))
            .WithMessage("El ID debe ser un ObjectId válido de 24 caracteres hexadecimales.");
    }
}
