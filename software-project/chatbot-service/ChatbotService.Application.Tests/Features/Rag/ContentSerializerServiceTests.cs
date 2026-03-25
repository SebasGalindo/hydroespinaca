using System.Text.Json;
using ChatbotService.Application.Features.Rag.Services;
using ChatbotService.Domain.Models.Fuzzy;
using FluentAssertions;
using Xunit;

namespace ChatbotService.Application.Tests.Features.Rag;

public class ContentSerializerServiceTests
{
    private readonly ContentSerializerService _sut = new();

    [Fact]
    public void SerializeSystem_ShouldFormatCorrectly()
    {
        var sys = new HydratedFuzzySystem("1", "Sistema Test", "Desc Test", "Activo", "Centroide", 
            new List<string> { "Temp" }, new List<string> { "Ventilador" }, new List<string> { "Regla 1" });

        var result = _sut.SerializeSystem(sys);

        result.Should().Contain("Sistema Test");
        result.Should().Contain("Activo");
        result.Should().Contain("Desc Test");
        result.Should().Contain("Centroide");
        result.Should().Contain("1 variables de entrada");
        result.Should().Contain("1 variables de salida");
        result.Should().Contain("1 reglas asociadas");
    }

    [Fact]
    public void SerializeVariable_ShouldFormatCorrectly()
    {
        var terms = new List<HydratedFuzzyTerm>
        {
            new HydratedFuzzyTerm("t1", "Alto", "triangular", new List<double> { 0, 10, 20 })
        };
        var v = new HydratedFuzzyVariable("v1", "Temp", "Sis 1", "input", "Desc Var", 0, 50, "T01", terms);

        var result = _sut.SerializeVariable(v);

        result.Should().Contain("entrada 'Temp'");
        result.Should().Contain("código de referencia 'T01'");
        result.Should().Contain("Sis 1");
        result.Should().Contain("0 a 50");
        result.Should().Contain("1 términos lingüísticos");
        result.Should().Contain("'Alto'");
        result.Should().Contain("triangular");
        result.Should().Contain("0, 10, 20");
    }

    [Fact]
    public void SerializeRule_ShouldFormatCorrectly()
    {
        var conds = new List<HydratedRuleCondition>
        {
            new HydratedRuleCondition("Temp", "IS", "Alto")
        };
        var cons = new List<HydratedRuleConsequent>
        {
            new HydratedRuleConsequent("Ventilador", new List<string> { "Rapido" }, "max")
        };
        var r = new HydratedFuzzyRule("r1", "Regla Enfriar", "Sis 1", "Desc Regla", "Si temp es alto", conds, new List<string> { "AND" }, cons);

        var result = _sut.SerializeRule(r);

        result.Should().Contain("Regla Enfriar");
        result.Should().Contain("Sis 1");
        result.Should().Contain("SI la variable 'Temp' IS 'Alto'");
        result.Should().Contain("ENTONCES la variable 'Ventilador' ES 'Rapido' (con agregación max)");
    }
}
