using BiService.Application.Features.Consumption.Commands.CreateConsumptionEntry;
using BiService.Domain.Enums;
using FluentValidation.TestHelper;

namespace BiService.Application.Tests.Features.Consumption.Validators;

public class CreateConsumptionEntryCommandValidatorTests
{
    private readonly CreateConsumptionEntryCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_ShouldPassValidation()
    {
        var cmd = new CreateConsumptionEntryCommand(
            DateTime.UtcNow.AddDays(-1), null, ConsumptionType.ElectricityKwh, 100m, null, "user-1");
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Amount_Zero_ShouldFail()
    {
        var cmd = new CreateConsumptionEntryCommand(
            DateTime.UtcNow.AddDays(-1), null, ConsumptionType.ElectricityKwh, 0m, null, "user-1");
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void Amount_Negative_ShouldFail()
    {
        var cmd = new CreateConsumptionEntryCommand(
            DateTime.UtcNow.AddDays(-1), null, ConsumptionType.ElectricityKwh, -10m, null, "user-1");
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void InvalidType_ShouldFail()
    {
        var cmd = new CreateConsumptionEntryCommand(
            DateTime.UtcNow.AddDays(-1), null, (ConsumptionType)99, 10m, null, "user-1");
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Type);
    }

    [Fact]
    public void DefaultDateFrom_ShouldFail()
    {
        var cmd = new CreateConsumptionEntryCommand(
            default, null, ConsumptionType.WaterLiters, 10m, null, "user-1");
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.DateFrom);
    }

    [Fact]
    public void DateTo_BeforeDateFrom_ShouldFail()
    {
        var cmd = new CreateConsumptionEntryCommand(
            DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(-5), ConsumptionType.WaterLiters, 10m, null, "user-1");
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.DateTo);
    }

    [Fact]
    public void EmptyUserId_ShouldFail()
    {
        var cmd = new CreateConsumptionEntryCommand(
            DateTime.UtcNow.AddDays(-1), null, ConsumptionType.WaterLiters, 10m, null, "");
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void DateTo_EqualToDateFrom_ShouldPass()
    {
        var date = DateTime.UtcNow.AddDays(-1);
        var cmd = new CreateConsumptionEntryCommand(date, date, ConsumptionType.WaterLiters, 10m, null, "user-1");
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveValidationErrorFor(x => x.DateTo);
    }
}
