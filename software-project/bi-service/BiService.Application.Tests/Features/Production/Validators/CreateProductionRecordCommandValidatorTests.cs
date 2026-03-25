using BiService.Application.Features.Production.Commands.CreateProductionRecord;
using FluentValidation.TestHelper;

namespace BiService.Application.Tests.Features.Production.Validators;

public class CreateProductionRecordCommandValidatorTests
{
    private readonly CreateProductionRecordCommandValidator _validator = new();

    private static CreateProductionRecordCommand ValidCommand() =>
        new("Espinaca Baby", DateTime.UtcNow.AddDays(-60), DateTime.UtcNow, 25.5m, 12000m, "COP", "Lote A", "user-1");

    [Fact]
    public void ValidCommand_ShouldPass()
    {
        var result = _validator.TestValidate(ValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyCropName_ShouldFail()
    {
        var cmd = ValidCommand() with { CropName = "" };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.CropName);
    }

    [Fact]
    public void CropNameTooLong_ShouldFail()
    {
        var cmd = ValidCommand() with { CropName = new string('A', 101) };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.CropName);
    }

    [Fact]
    public void DefaultStartDate_ShouldFail()
    {
        var cmd = ValidCommand() with { StartDate = default };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.StartDate);
    }

    [Fact]
    public void DefaultHarvestDate_ShouldFail()
    {
        var cmd = ValidCommand() with { HarvestDate = default };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.HarvestDate);
    }

    [Fact]
    public void StartDateAfterHarvestDate_ShouldFail()
    {
        var cmd = ValidCommand() with
        {
            StartDate = DateTime.UtcNow,
            HarvestDate = DateTime.UtcNow.AddDays(-30)
        };
        var result = _validator.TestValidate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ZeroKilosProduced_ShouldFail()
    {
        var cmd = ValidCommand() with { KilosProduced = 0 };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.KilosProduced);
    }

    [Fact]
    public void NegativeKilosProduced_ShouldFail()
    {
        var cmd = ValidCommand() with { KilosProduced = -1m };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.KilosProduced);
    }

    [Fact]
    public void NegativePricePerKilo_ShouldFail()
    {
        var cmd = ValidCommand() with { PricePerKilo = -1m };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.PricePerKilo);
    }

    [Fact]
    public void ZeroPricePerKilo_ShouldPass()
    {
        var cmd = ValidCommand() with { PricePerKilo = 0m };
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyCurrency_ShouldFail()
    {
        var cmd = ValidCommand() with { Currency = "" };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Currency);
    }

    [Fact]
    public void CurrencyTooLong_ShouldFail()
    {
        var cmd = ValidCommand() with { Currency = "ABCDEFGHIJK" };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Currency);
    }

    [Fact]
    public void EmptyUserId_ShouldFail()
    {
        var cmd = ValidCommand() with { UserId = "" };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void NullNote_ShouldPass()
    {
        var cmd = ValidCommand() with { Note = null };
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
