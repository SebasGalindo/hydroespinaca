using HydroEspinaca.Shared.Enums;
using SensorService.Domain.Entities;

namespace SensorService.Domain.Tests.Builders;

public class VariableBuilder
{
    private string _id = "12345abcdef1234567890abc";
    private string _code = "PH";
    private string _name = "pH";
    private string _unit = "";
    private string _description = "Nivel de pH del agua";
    private double _physicalMin = 0;
    private double _physicalMax = 14;
    private double _optimalMin = 6.0;
    private double? _optimalMax = 7.5;
    private VariableTypes _type = VariableTypes.Analog;
    private RegulationType? _regulationType = RegulationType.Manual;

    public VariableBuilder WithId(string id)
    {
        _id = id;
        return this;
    }

    public VariableBuilder WithCode(string code)
    {
        _code = code;
        return this;
    }

    public VariableBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public VariableBuilder WithUnit(string unit)
    {
        _unit = unit;
        return this;
    }

    public VariableBuilder WithPhysicalMin(double min)
    {
        _physicalMin = min;
        return this;
    }

    public VariableBuilder WithPhysicalMax(double max)
    {
        _physicalMax = max;
        return this;
    }

    public VariableBuilder WithOptimalMin(double min)
    {
        _optimalMin = min;
        return this;
    }

    public VariableBuilder WithOptimalMax(double? max)
    {
        _optimalMax = max;
        return this;
    }

    public VariableBuilder WithType(VariableTypes type)
    {
        _type = type;
        return this;
    }

    public VariableBuilder WithRegulationType(RegulationType? regulationType)
    {
        _regulationType = regulationType;
        return this;
    }

    public Variable Build()
    {
        var variable = new Variable
        {
            Code = _code,
            Name = _name,
            Unit = _unit,
            Description = _description,
            PhysicalMin = _physicalMin,
            PhysicalMax = _physicalMax,
            OptimalMin = _optimalMin,
            OptimalMax = _optimalMax,
            Type = _type,
            RegulationType = _regulationType
        };
        variable.SetId(_id);
        return variable;
    }
}
