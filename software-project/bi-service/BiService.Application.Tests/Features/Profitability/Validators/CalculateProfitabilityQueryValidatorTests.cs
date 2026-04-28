using BiService.Application.DTOs.OperationalCost;
using BiService.Application.Features.Profitability.Queries.CalculateProfitability;
using FluentValidation.TestHelper;

namespace BiService.Application.Tests.Features.Profitability.Validators;

public class CalculateProfitabilityQueryValidatorTests
{
    private readonly CalculateProfitabilityQueryValidator _validator = new();

    private static List<ActuatorDurationInput> SampleDurations() =>
        new()
        {
            new ActuatorDurationInput { ActuatorCode = "PUMP-1", PowerConsumptionWatts = 100m, TotalDurationSeconds = 3600 }
        };

    [Fact]
    public void ValidQuery_ShouldPass()
    {
        var query = new CalculateProfitabilityQuery("record-1", SampleDurations());
        var result = _validator.TestValidate(query);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyProductionRecordId_ShouldFail()
    {
        var query = new CalculateProfitabilityQuery("", SampleDurations());
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.ProductionRecordId);
    }

    [Fact]
    public void EmptyActuatorDurations_ShouldPass()
    {
        var query = new CalculateProfitabilityQuery("record-1", new List<ActuatorDurationInput>());
        var result = _validator.TestValidate(query);
        result.ShouldNotHaveValidationErrorFor(x => x.ActuatorDurations);
    }

    [Fact]
    public void NullActuatorDurations_ShouldFail()
    {
        var query = new CalculateProfitabilityQuery("record-1", null!);
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.ActuatorDurations);
    }

    [Fact]
    public void MultipleActuatorDurations_ShouldPass()
    {
        var durations = new List<ActuatorDurationInput>
        {
            new() { ActuatorCode = "PUMP-1", PowerConsumptionWatts = 100m, TotalDurationSeconds = 3600 },
            new() { ActuatorCode = "LIGHT-1", PowerConsumptionWatts = 50m, TotalDurationSeconds = 7200 }
        };
        var query = new CalculateProfitabilityQuery("record-1", durations);
        var result = _validator.TestValidate(query);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ValidInitialInvestmentCost_ShouldPass()
    {
        var query = new CalculateProfitabilityQuery("record-1", SampleDurations(), InitialInvestmentCost: 850000m);
        var result = _validator.TestValidate(query);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void NegativeInitialInvestmentCost_ShouldFail()
    {
        var query = new CalculateProfitabilityQuery("record-1", SampleDurations(), InitialInvestmentCost: -1m);
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.InitialInvestmentCost);
    }

    [Fact]
    public void NullInitialInvestmentCost_ShouldPass()
    {
        var query = new CalculateProfitabilityQuery("record-1", SampleDurations(), InitialInvestmentCost: null);
        var result = _validator.TestValidate(query);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
