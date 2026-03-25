using BiService.Domain.Enums;

namespace BiService.Domain.Tests.Enums;

public class ConsumptionTypeTests
{
    [Fact]
    public void ElectricityKwh_ShouldHaveValue1()
    {
        ((int)ConsumptionType.ElectricityKwh).Should().Be(1);
    }

    [Fact]
    public void WaterLiters_ShouldHaveValue2()
    {
        ((int)ConsumptionType.WaterLiters).Should().Be(2);
    }

    [Fact]
    public void NutrientLiters_ShouldHaveValue3()
    {
        ((int)ConsumptionType.NutrientLiters).Should().Be(3);
    }

    [Fact]
    public void ConsumptionType_ShouldHaveExactlyThreeValues()
    {
        Enum.GetValues<ConsumptionType>().Should().HaveCount(3);
    }

    [Theory]
    [InlineData("ElectricityKwh", ConsumptionType.ElectricityKwh)]
    [InlineData("WaterLiters", ConsumptionType.WaterLiters)]
    [InlineData("NutrientLiters", ConsumptionType.NutrientLiters)]
    public void ConsumptionType_ShouldParseFromString(string name, ConsumptionType expected)
    {
        Enum.Parse<ConsumptionType>(name).Should().Be(expected);
    }

    [Fact]
    public void ConsumptionType_InvalidValue_ShouldNotBeDefined()
    {
        Enum.IsDefined(typeof(ConsumptionType), 99).Should().BeFalse();
    }
}
