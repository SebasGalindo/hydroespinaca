using BiService.Domain.Entities;
using BiService.Domain.Enums;

namespace BiService.Domain.Tests.Entities;

public class ManualConsumptionEntryTests
{
    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        var entity = new ManualConsumptionEntry();

        entity.Id.Should().BeEmpty();
        entity.CurrencySnapshot.Should().Be("COP");
        entity.CostConfigVersionId.Should().BeEmpty();
        entity.Note.Should().BeNull();
        entity.CreatedByUserId.Should().BeEmpty();
    }

    [Fact]
    public void SetId_ShouldAssignId()
    {
        var entity = new ManualConsumptionEntry();
        entity.SetId("entry-1");
        entity.Id.Should().Be("entry-1");
    }

    [Fact]
    public void SetId_CanBeCalledMultipleTimes()
    {
        var entity = new ManualConsumptionEntry();
        entity.SetId("first");
        entity.SetId("second");
        entity.Id.Should().Be("second");
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        var now = DateTime.UtcNow;
        var entity = new ManualConsumptionEntry
        {
            DateFrom = now.AddDays(-7),
            DateTo = now,
            Type = ConsumptionType.WaterLiters,
            Amount = 500m,
            UnitCostSnapshot = 3.2m,
            CurrencySnapshot = "USD",
            CostConfigVersionId = "config-v1",
            CostAmount = 1600m,
            Note = "Weekly water usage",
            CreatedAt = now,
            CreatedByUserId = "user-42"
        };

        entity.DateFrom.Should().Be(now.AddDays(-7));
        entity.DateTo.Should().Be(now);
        entity.Type.Should().Be(ConsumptionType.WaterLiters);
        entity.Amount.Should().Be(500m);
        entity.UnitCostSnapshot.Should().Be(3.2m);
        entity.CurrencySnapshot.Should().Be("USD");
        entity.CostConfigVersionId.Should().Be("config-v1");
        entity.CostAmount.Should().Be(1600m);
        entity.Note.Should().Be("Weekly water usage");
        entity.CreatedByUserId.Should().Be("user-42");
    }

    [Theory]
    [InlineData(ConsumptionType.ElectricityKwh)]
    [InlineData(ConsumptionType.WaterLiters)]
    [InlineData(ConsumptionType.NutrientLiters)]
    public void Type_ShouldAcceptAllConsumptionTypes(ConsumptionType type)
    {
        var entity = new ManualConsumptionEntry { Type = type };
        entity.Type.Should().Be(type);
    }

    [Fact]
    public void CostAmount_ShouldBeCalculableFromAmountAndUnitCost()
    {
        var amount = 100m;
        var unitCost = 5.5m;
        var entity = new ManualConsumptionEntry
        {
            Amount = amount,
            UnitCostSnapshot = unitCost,
            CostAmount = decimal.Round(amount * unitCost, 4)
        };

        entity.CostAmount.Should().Be(550m);
    }

    [Fact]
    public void CreatedAt_Default_ShouldBeCloseToNow()
    {
        var before = DateTime.UtcNow;
        var entity = new ManualConsumptionEntry();
        var after = DateTime.UtcNow;

        entity.CreatedAt.Should().BeOnOrAfter(before);
        entity.CreatedAt.Should().BeOnOrBefore(after);
    }
}
