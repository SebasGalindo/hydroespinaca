using BiService.Application.Features.CostConfig.Commands.CreateCostConfigVersion;
using FluentValidation.TestHelper;

namespace BiService.Application.Tests.Features.CostConfig.Validators;

public class CreateCostConfigVersionCommandValidatorTests
{
    private readonly CreateCostConfigVersionCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_ShouldPass()
    {
        var cmd = new CreateCostConfigVersionCommand("COP", 800m, 5m, 12m, null, null, "user-1");
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyCurrency_ShouldFail()
    {
        var cmd = new CreateCostConfigVersionCommand("", 800m, 5m, 12m, null, null, "user-1");
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Currency);
    }

    [Fact]
    public void CurrencyTooLong_ShouldFail()
    {
        var cmd = new CreateCostConfigVersionCommand("ABCDEFGHIJK", 800m, 5m, 12m, null, null, "user-1");
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Currency);
    }

    [Fact]
    public void NegativeElectricityCost_ShouldFail()
    {
        var cmd = new CreateCostConfigVersionCommand("COP", -1m, 5m, 12m, null, null, "user-1");
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.ElectricityCostPerKwh);
    }

    [Fact]
    public void NegativeWaterCost_ShouldFail()
    {
        var cmd = new CreateCostConfigVersionCommand("COP", 800m, -1m, 12m, null, null, "user-1");
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.WaterCostPerLiter);
    }

    [Fact]
    public void NegativeNutrientCost_ShouldFail()
    {
        var cmd = new CreateCostConfigVersionCommand("COP", 800m, 5m, -1m, null, null, "user-1");
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.NutrientCostPerLiter);
    }

    [Fact]
    public void EmptyUserId_ShouldFail()
    {
        var cmd = new CreateCostConfigVersionCommand("COP", 800m, 5m, 12m, null, null, "");
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void ZeroCosts_ShouldPass()
    {
        var cmd = new CreateCostConfigVersionCommand("COP", 0m, 0m, 0m, null, null, "user-1");
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
