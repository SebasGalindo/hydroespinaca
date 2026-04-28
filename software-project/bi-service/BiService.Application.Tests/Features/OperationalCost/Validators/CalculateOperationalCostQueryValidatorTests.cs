using BiService.Application.DTOs.OperationalCost;
using BiService.Application.Features.OperationalCost.Queries.CalculateOperationalCost;
using FluentValidation.TestHelper;

namespace BiService.Application.Tests.Features.OperationalCost.Validators;

public class CalculateOperationalCostQueryValidatorTests
{
    private readonly CalculateOperationalCostQueryValidator _validator = new();

    private static List<ActuatorDurationInput> SampleDurations() =>
        new()
        {
            new ActuatorDurationInput { ActuatorCode = "PUMP-1", PowerConsumptionWatts = 100m, TotalDurationSeconds = 3600 }
        };

    [Fact]
    public void ValidQuery_ShouldPass()
    {
        var query = new CalculateOperationalCostQuery(
            DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow,
            SampleDurations());

        var result = _validator.TestValidate(query);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void DefaultFrom_ShouldFail()
    {
        var query = new CalculateOperationalCostQuery(
            default,
            DateTime.UtcNow,
            SampleDurations());

        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.From);
    }

    [Fact]
    public void DefaultTo_ShouldFail()
    {
        var query = new CalculateOperationalCostQuery(
            DateTime.UtcNow.AddDays(-30),
            default,
            SampleDurations());

        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.To);
    }

    [Fact]
    public void FromAfterTo_ShouldFail()
    {
        var query = new CalculateOperationalCostQuery(
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(-30),
            SampleDurations());

        var result = _validator.TestValidate(query);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void EmptyActuatorDurations_ShouldPass()
    {
        var query = new CalculateOperationalCostQuery(
            DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow,
            new List<ActuatorDurationInput>());

        var result = _validator.TestValidate(query);
        result.ShouldNotHaveValidationErrorFor(x => x.ActuatorDurations);
    }

    [Fact]
    public void NullActuatorDurations_ShouldFail()
    {
        var query = new CalculateOperationalCostQuery(
            DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow,
            null!);

        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.ActuatorDurations);
    }

    [Fact]
    public void FromEqualsTo_ShouldPass()
    {
        var now = DateTime.UtcNow;
        var query = new CalculateOperationalCostQuery(now, now, SampleDurations());

        var result = _validator.TestValidate(query);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
