using HydroEspinaca.Shared.Enums;
using SensorService.Domain.Entities;

namespace SensorService.Domain.Tests.Builders;

public class SensorBuilder
{
    private string _id = "sensor123abc456def789ghi";
    private string _code = "DHT22-01";
    private string _physicalId = "DHT22-A1";
    private string _location = "Greenhouse Zone A";
    private string _esp32Id = "esp32-01";
    private List<string> _variables = new() { "T_AMB", "HUM" };
    private int _samplingFrequency = 60;
    private SensorStatus _status = SensorStatus.Active;
    private bool _allowMissing = false;

    public SensorBuilder WithId(string id)
    {
        _id = id;
        return this;
    }

    public SensorBuilder WithCode(string code)
    {
        _code = code;
        return this;
    }

    public SensorBuilder WithPhysicalId(string physicalId)
    {
        _physicalId = physicalId;
        return this;
    }

    public SensorBuilder WithLocation(string location)
    {
        _location = location;
        return this;
    }

    public SensorBuilder WithEsp32Id(string esp32Id)
    {
        _esp32Id = esp32Id;
        return this;
    }

    public SensorBuilder WithVariables(List<string> variables)
    {
        _variables = variables;
        return this;
    }

    public SensorBuilder WithSamplingFrequency(int frequency)
    {
        _samplingFrequency = frequency;
        return this;
    }

    public SensorBuilder WithStatus(SensorStatus status)
    {
        _status = status;
        return this;
    }

    public SensorBuilder WithAllowMissing(bool allowMissing)
    {
        _allowMissing = allowMissing;
        return this;
    }

    public Sensor Build()
    {
        var sensor = new Sensor
        {
            Code = _code,
            PhysicalId = _physicalId,
            Location = _location,
            Esp32Id = _esp32Id,
            Variables = _variables,
            SamplingFrequency = _samplingFrequency,
            Status = _status,
            AllowMissing = _allowMissing
        };
        sensor.SetId(_id);
        return sensor;
    }
}
