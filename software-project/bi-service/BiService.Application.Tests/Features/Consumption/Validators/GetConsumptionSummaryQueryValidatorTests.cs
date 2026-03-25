using BiService.Application.Features.Consumption.Queries.GetConsumptionSummary;
using FluentValidation.TestHelper;

namespace BiService.Application.Tests.Features.Consumption.Validators;

public class GetConsumptionSummaryQueryValidatorTests
{
    private readonly GetConsumptionSummaryQueryValidator _validator = new();

    [Fact]
    public void ValidQuery_ShouldPass()
    {
        var query = new GetConsumptionSummaryQuery(new DateTime(2025, 1, 1), new DateTime(2025, 3, 1));
        var result = _validator.TestValidate(query);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void DefaultFrom_ShouldFail()
    {
        var query = new GetConsumptionSummaryQuery(default, new DateTime(2025, 3, 1));
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.From);
    }

    [Fact]
    public void DefaultTo_ShouldFail()
    {
        var query = new GetConsumptionSummaryQuery(new DateTime(2025, 1, 1), default);
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.To);
    }

    [Fact]
    public void FromAfterTo_ShouldFail()
    {
        var query = new GetConsumptionSummaryQuery(new DateTime(2025, 6, 1), new DateTime(2025, 1, 1));
        var result = _validator.TestValidate(query);
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("'from' debe ser menor o igual a 'to'"));
    }
}
