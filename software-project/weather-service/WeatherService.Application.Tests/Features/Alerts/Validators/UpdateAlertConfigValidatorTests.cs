using WeatherService.Application.Features.Alerts.Commands.UpdateAlertConfig;
using FluentValidation.TestHelper;

namespace WeatherService.Application.Tests.Features.Alerts.Validators;

public class UpdateAlertConfigValidatorTests
{
    private readonly UpdateAlertConfigValidator _validator = new();

    [Fact]
    public void ValidCommand_ShouldPass()
    {
        var cmd = new UpdateAlertConfigCommand(
            "fuzzy-1", "user-1", true,
            new List<AlertThresholdDto>
            {
                new("extreme_heat", true, 35, "gt", "Cool down plants")
            });

        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyFuzzySystemId_ShouldFail()
    {
        var cmd = new UpdateAlertConfigCommand(
            "", "user-1", true, new List<AlertThresholdDto>());

        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.FuzzySystemId);
    }

    [Fact]
    public void EmptyUserId_ShouldFail()
    {
        var cmd = new UpdateAlertConfigCommand(
            "fuzzy-1", "", true, new List<AlertThresholdDto>());

        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void InvalidAlertType_ShouldFail()
    {
        var cmd = new UpdateAlertConfigCommand(
            "fuzzy-1", "user-1", true,
            new List<AlertThresholdDto>
            {
                new("invalid_type", true, 35, "gt", "Some recommendation")
            });

        var result = _validator.TestValidate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void InvalidComparison_ShouldFail()
    {
        var cmd = new UpdateAlertConfigCommand(
            "fuzzy-1", "user-1", true,
            new List<AlertThresholdDto>
            {
                new("extreme_heat", true, 35, "eq", "Some recommendation")
            });

        var result = _validator.TestValidate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void NullComparison_ShouldPass()
    {
        var cmd = new UpdateAlertConfigCommand(
            "fuzzy-1", "user-1", true,
            new List<AlertThresholdDto>
            {
                new("thunderstorm", true, null, null, "Check electrical connections")
            });

        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyRecommendation_ShouldFail()
    {
        var cmd = new UpdateAlertConfigCommand(
            "fuzzy-1", "user-1", true,
            new List<AlertThresholdDto>
            {
                new("extreme_heat", true, 35, "gt", "")
            });

        var result = _validator.TestValidate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void EmptyAlertsList_ShouldPass()
    {
        var cmd = new UpdateAlertConfigCommand(
            "fuzzy-1", "user-1", true,
            new List<AlertThresholdDto>());

        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
