using BiService.Application.Features.CostConfig.Commands.UpdateCostConfigVersion;
using FluentValidation.TestHelper;

namespace BiService.Application.Tests.Features.CostConfig.Validators;

public class UpdateCostConfigVersionCommandValidatorTests
{
    private readonly UpdateCostConfigVersionCommandValidator _validator = new();

    private static UpdateCostConfigVersionCommand ValidCommand() =>
        new("config-1", "COP", 800m, 5m, 12m, null, null, "user-1");

    [Fact]
    public void ValidCommand_ShouldPass()
    {
        var result = _validator.TestValidate(ValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyId_ShouldFail()
    {
        var cmd = ValidCommand() with { Id = "" };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Id);
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
    public void NegativeElectricityCost_ShouldFail()
    {
        var cmd = ValidCommand() with { ElectricityCostPerKwh = -0.01m };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.ElectricityCostPerKwh);
    }

    [Fact]
    public void NegativeWaterCost_ShouldFail()
    {
        var cmd = ValidCommand() with { WaterCostPerLiter = -1m };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.WaterCostPerLiter);
    }

    [Fact]
    public void NegativeNutrientCost_ShouldFail()
    {
        var cmd = ValidCommand() with { NutrientCostPerLiter = -1m };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.NutrientCostPerLiter);
    }

    [Fact]
    public void EmptyUserId_ShouldFail()
    {
        var cmd = ValidCommand() with { UserId = "" };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void ZeroCosts_ShouldPass()
    {
        var cmd = ValidCommand() with
        {
            ElectricityCostPerKwh = 0m,
            WaterCostPerLiter = 0m,
            NutrientCostPerLiter = 0m
        };
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
