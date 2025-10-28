using SensorService.Domain.Entities;

namespace SensorService.Domain.Tests.Builders;

public class ReadingBuilder
{
    private string _id = "67890abcdef1234567890abc";
    private string _sensorCode = "SEN-01";
    private string _variableCode = "PH";
    private double _value = 6.5;
    private DateTime _timestamp = DateTime.UtcNow;

    public ReadingBuilder WithId(string id)
    {
        _id = id;
        return this;
    }

    public ReadingBuilder WithSensorCode(string code)
    {
        _sensorCode = code;
        return this;
    }

    public ReadingBuilder WithVariableCode(string code)
    {
        _variableCode = code;
        return this;
    }

    public ReadingBuilder WithValue(double value)
    {
        _value = value;
        return this;
    }

    public ReadingBuilder WithTimestamp(DateTime timestamp)
    {
        _timestamp = timestamp;
        return this;
    }

    public Reading Build()
    {
        var reading = new Reading
        {
            SensorCode = _sensorCode,
            VariableCode = _variableCode,
            Value = _value,
            Timestamp = _timestamp
        };
        reading.SetId(_id);
        return reading;
    }
}
