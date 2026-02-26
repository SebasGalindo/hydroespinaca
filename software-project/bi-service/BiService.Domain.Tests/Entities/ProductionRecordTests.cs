using BiService.Domain.Entities;

namespace BiService.Domain.Tests.Entities;

public class ProductionRecordTests
{
    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        var entity = new ProductionRecord();

        entity.Id.Should().BeEmpty();
        entity.CropName.Should().BeEmpty();
        entity.Currency.Should().Be("COP");
        entity.Note.Should().BeNull();
        entity.CreatedByUserId.Should().BeEmpty();
    }

    [Fact]
    public void SetId_ShouldAssignId()
    {
        var entity = new ProductionRecord();
        entity.SetId("prod-1");
        entity.Id.Should().Be("prod-1");
    }

    [Fact]
    public void SetId_CanBeCalledMultipleTimes()
    {
        var entity = new ProductionRecord();
        entity.SetId("first");
        entity.SetId("second");
        entity.Id.Should().Be("second");
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        var now = DateTime.UtcNow;
        var entity = new ProductionRecord
        {
            CropName = "Espinaca",
            StartDate = now.AddDays(-30),
            HarvestDate = now,
            KilosProduced = 25.5m,
            PricePerKilo = 12000m,
            Currency = "COP",
            Note = "First harvest",
            CreatedAt = now,
            CreatedByUserId = "user-1"
        };

        entity.CropName.Should().Be("Espinaca");
        entity.StartDate.Should().Be(now.AddDays(-30));
        entity.HarvestDate.Should().Be(now);
        entity.KilosProduced.Should().Be(25.5m);
        entity.PricePerKilo.Should().Be(12000m);
        entity.Currency.Should().Be("COP");
        entity.Note.Should().Be("First harvest");
        entity.CreatedByUserId.Should().Be("user-1");
    }

    [Fact]
    public void Revenue_ShouldBeCalculableFromKilosAndPrice()
    {
        var entity = new ProductionRecord
        {
            KilosProduced = 10m,
            PricePerKilo = 15000m
        };

        var revenue = entity.KilosProduced * entity.PricePerKilo;
        revenue.Should().Be(150000m);
    }

    [Fact]
    public void CreatedAt_Default_ShouldBeCloseToNow()
    {
        var before = DateTime.UtcNow;
        var entity = new ProductionRecord();
        var after = DateTime.UtcNow;

        entity.CreatedAt.Should().BeOnOrAfter(before);
        entity.CreatedAt.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void Note_ShouldBeNullable()
    {
        var entity = new ProductionRecord { Note = null };
        entity.Note.Should().BeNull();

        entity.Note = "Some note";
        entity.Note.Should().Be("Some note");
    }
}
