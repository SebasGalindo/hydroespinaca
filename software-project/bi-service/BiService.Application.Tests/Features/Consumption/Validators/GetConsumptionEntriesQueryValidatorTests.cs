using BiService.Application.Features.Consumption.Queries.GetConsumptionEntries;
using BiService.Domain.Enums;
using FluentValidation.TestHelper;

namespace BiService.Application.Tests.Features.Consumption.Validators;

public class GetConsumptionEntriesQueryValidatorTests
{
    private readonly GetConsumptionEntriesQueryValidator _validator = new();

    [Fact]
    public void ValidQuery_ShouldPass()
    {
        var query = new GetConsumptionEntriesQuery(new DateTime(2025, 1, 1), new DateTime(2025, 3, 1), null);
        var result = _validator.TestValidate(query);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void DefaultFrom_ShouldFail()
    {
        var query = new GetConsumptionEntriesQuery(default, new DateTime(2025, 3, 1), null);
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.From);
    }

    [Fact]
    public void DefaultTo_ShouldFail()
    {
        var query = new GetConsumptionEntriesQuery(new DateTime(2025, 1, 1), default, null);
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.To);
    }

    [Fact]
    public void FromAfterTo_ShouldFail()
    {
        var query = new GetConsumptionEntriesQuery(new DateTime(2025, 6, 1), new DateTime(2025, 1, 1), null);
        var result = _validator.TestValidate(query);
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("'from' debe ser menor o igual a 'to'"));
    }

    [Fact]
    public void WithValidType_ShouldPass()
    {
        var query = new GetConsumptionEntriesQuery(new DateTime(2025, 1, 1), new DateTime(2025, 3, 1), ConsumptionType.WaterLiters);
        var result = _validator.TestValidate(query);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void WithInvalidType_ShouldFail()
    {
        var query = new GetConsumptionEntriesQuery(new DateTime(2025, 1, 1), new DateTime(2025, 3, 1), (ConsumptionType)99);
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.Type);
    }
}
