using BiService.Domain.Entities;

namespace BiService.Domain.Tests.Entities;

public class CostConfigVersionTests
{
    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        var entity = new CostConfigVersion();

        entity.Id.Should().BeEmpty();
        entity.Currency.Should().Be("COP");
        entity.ElectricityCostPerKwh.Should().Be(0m);
        entity.WaterCostPerLiter.Should().Be(0m);
        entity.NutrientCostPerLiter.Should().Be(0m);
        entity.EffectiveTo.Should().BeNull();
        entity.IsActive.Should().BeTrue();
        entity.CreatedByUserId.Should().BeEmpty();
    }

    [Fact]
    public void SetId_ShouldAssignId()
    {
        var entity = new CostConfigVersion();
        entity.SetId("abc-123");
        entity.Id.Should().Be("abc-123");
    }

    [Fact]
    public void SetId_WithEmptyString_ShouldAssignEmpty()
    {
        var entity = new CostConfigVersion();
        entity.SetId("first");
        entity.SetId("");
        entity.Id.Should().BeEmpty();
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        var now = DateTime.UtcNow;
        var entity = new CostConfigVersion
        {
            Currency = "USD",
            ElectricityCostPerKwh = 150.5m,
            WaterCostPerLiter = 3.2m,
            NutrientCostPerLiter = 8.75m,
            EffectiveFrom = now,
            EffectiveTo = now.AddMonths(1),
            IsActive = false,
            CreatedAt = now,
            CreatedByUserId = "user-1"
        };

        entity.Currency.Should().Be("USD");
        entity.ElectricityCostPerKwh.Should().Be(150.5m);
        entity.WaterCostPerLiter.Should().Be(3.2m);
        entity.NutrientCostPerLiter.Should().Be(8.75m);
        entity.EffectiveFrom.Should().Be(now);
        entity.EffectiveTo.Should().Be(now.AddMonths(1));
        entity.IsActive.Should().BeFalse();
        entity.CreatedAt.Should().Be(now);
        entity.CreatedByUserId.Should().Be("user-1");
    }

    [Fact]
    public void EffectiveTo_WhenNull_ShouldIndicateActiveVersion()
    {
        var entity = new CostConfigVersion
        {
            EffectiveFrom = DateTime.UtcNow,
            EffectiveTo = null,
            IsActive = true
        };

        entity.EffectiveTo.Should().BeNull();
        entity.IsActive.Should().BeTrue();
    }

    [Fact]
    public void CreatedAt_Default_ShouldBeCloseToNow()
    {
        var before = DateTime.UtcNow;
        var entity = new CostConfigVersion();
        var after = DateTime.UtcNow;

        entity.CreatedAt.Should().BeOnOrAfter(before);
        entity.CreatedAt.Should().BeOnOrBefore(after);
    }
}
