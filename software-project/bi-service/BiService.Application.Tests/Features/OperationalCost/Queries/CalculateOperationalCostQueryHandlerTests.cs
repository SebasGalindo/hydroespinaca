using BiService.Application.DTOs.OperationalCost;
using BiService.Application.Features.OperationalCost.Queries.CalculateOperationalCost;
using BiService.Domain.Entities;
using BiService.Domain.Interfaces;

namespace BiService.Application.Tests.Features.OperationalCost.Queries;

public class CalculateOperationalCostQueryHandlerTests
{
    private readonly Mock<ICostConfigVersionRepository> _costConfigRepo = new();
    private readonly CalculateOperationalCostQueryHandler _handler;

    public CalculateOperationalCostQueryHandlerTests()
    {
        _handler = new CalculateOperationalCostQueryHandler(_costConfigRepo.Object);
    }

    [Fact]
    public async Task Handle_SingleVersion_ShouldCalculateCorrectly()
    {
        var config = new CostConfigVersion
        {
            ElectricityCostPerKwh = 800m,
            Currency = "COP",
            EffectiveFrom = new DateTime(2025, 1, 1),
            IsActive = true
        };
        config.SetId("v-1");
        _costConfigRepo.Setup(r => r.GetVersionsForRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CostConfigVersion> { config });

        var actuators = new List<ActuatorDurationInput>
        {
            new() { ActuatorCode = "PUMP-1", PowerConsumptionWatts = 100m, TotalDurationSeconds = 3600, ActivationCount = 5 }
        };
        var query = new CalculateOperationalCostQuery(new DateTime(2025, 1, 1), new DateTime(2025, 1, 31), actuators);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Currency.Should().Be("COP");
        result.Actuators.Should().HaveCount(1);
        result.Actuators[0].ActuatorCode.Should().Be("PUMP-1");
        result.Actuators[0].TotalHours.Should().Be(1m);
        // 1 hour * 100W / 1000 = 0.1 kWh * 800 = 80 COP
        result.Actuators[0].EstimatedKwh.Should().Be(0.1m);
        result.Actuators[0].EstimatedCost.Should().Be(80m);
        result.TotalEstimatedKwh.Should().Be(0.1m);
        result.TotalOperationalCost.Should().Be(80m);
    }

    [Fact]
    public async Task Handle_MultipleActuators_ShouldAggregateTotal()
    {
        var config = new CostConfigVersion
        {
            ElectricityCostPerKwh = 1000m,
            Currency = "COP",
            EffectiveFrom = new DateTime(2025, 1, 1)
        };
        config.SetId("v-1");
        _costConfigRepo.Setup(r => r.GetVersionsForRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CostConfigVersion> { config });

        var actuators = new List<ActuatorDurationInput>
        {
            new() { ActuatorCode = "PUMP-1", PowerConsumptionWatts = 500m, TotalDurationSeconds = 7200, ActivationCount = 10 },
            new() { ActuatorCode = "FAN-1", PowerConsumptionWatts = 200m, TotalDurationSeconds = 3600, ActivationCount = 3 }
        };
        var query = new CalculateOperationalCostQuery(new DateTime(2025, 1, 1), new DateTime(2025, 1, 31), actuators);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Actuators.Should().HaveCount(2);
        result.TotalEstimatedKwh.Should().Be(result.Actuators[0].EstimatedKwh + result.Actuators[1].EstimatedKwh);
        result.TotalOperationalCost.Should().Be(result.Actuators[0].EstimatedCost + result.Actuators[1].EstimatedCost);
    }

    [Fact]
    public async Task Handle_NoVersionsForRange_FallbackToCurrent_ShouldWork()
    {
        _costConfigRepo.Setup(r => r.GetVersionsForRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CostConfigVersion>());

        var current = new CostConfigVersion { ElectricityCostPerKwh = 500m, Currency = "COP", EffectiveFrom = new DateTime(2025, 1, 1) };
        current.SetId("v-current");
        _costConfigRepo.Setup(r => r.GetCurrentAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(current);

        var actuators = new List<ActuatorDurationInput>
        {
            new() { ActuatorCode = "PUMP-1", PowerConsumptionWatts = 100m, TotalDurationSeconds = 3600, ActivationCount = 1 }
        };
        var query = new CalculateOperationalCostQuery(new DateTime(2025, 1, 1), new DateTime(2025, 1, 31), actuators);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.CostConfigPeriodsUsed.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_NoVersionsAtAll_ShouldThrowInvalidOperation()
    {
        _costConfigRepo.Setup(r => r.GetVersionsForRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CostConfigVersion>());
        _costConfigRepo.Setup(r => r.GetCurrentAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((CostConfigVersion?)null);

        var actuators = new List<ActuatorDurationInput>
        {
            new() { ActuatorCode = "PUMP-1", PowerConsumptionWatts = 100m, TotalDurationSeconds = 3600, ActivationCount = 1 }
        };
        var query = new CalculateOperationalCostQuery(new DateTime(2025, 1, 1), new DateTime(2025, 1, 31), actuators);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ShouldReturnFromToAndPeriodsUsed()
    {
        var config = new CostConfigVersion
        {
            ElectricityCostPerKwh = 800m, Currency = "COP",
            EffectiveFrom = new DateTime(2025, 1, 1)
        };
        config.SetId("v-1");
        _costConfigRepo.Setup(r => r.GetVersionsForRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CostConfigVersion> { config });

        var from = new DateTime(2025, 2, 1);
        var to = new DateTime(2025, 2, 28);
        var actuators = new List<ActuatorDurationInput>
        {
            new() { ActuatorCode = "X", PowerConsumptionWatts = 10m, TotalDurationSeconds = 100, ActivationCount = 1 }
        };
        var query = new CalculateOperationalCostQuery(from, to, actuators);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.From.Should().Be(from);
        result.To.Should().Be(to);
        result.CostConfigPeriodsUsed.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_ZeroDurationActuator_ShouldReturnZeroCost()
    {
        var config = new CostConfigVersion { ElectricityCostPerKwh = 800m, Currency = "COP", EffectiveFrom = new DateTime(2025, 1, 1) };
        config.SetId("v-1");
        _costConfigRepo.Setup(r => r.GetVersionsForRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CostConfigVersion> { config });

        var actuators = new List<ActuatorDurationInput>
        {
            new() { ActuatorCode = "PUMP-1", PowerConsumptionWatts = 100m, TotalDurationSeconds = 0, ActivationCount = 0 }
        };
        var query = new CalculateOperationalCostQuery(new DateTime(2025, 1, 1), new DateTime(2025, 1, 31), actuators);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Actuators[0].EstimatedKwh.Should().Be(0m);
        result.Actuators[0].EstimatedCost.Should().Be(0m);
    }
}
