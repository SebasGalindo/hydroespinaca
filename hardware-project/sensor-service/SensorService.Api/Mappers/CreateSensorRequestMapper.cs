using SensorService.Api.Dtos;
using SensorService.Domain.Entities;

namespace SensorService.Api.Mappers;
public static class CreateSensorRequestMapper
{
    public static Sensor ToDomain(this CreateSensorRequest dto)
    {
        return new Sensor
        {
            Code = dto.Code,
            Type = dto.Type,
            Unit = dto.Unit,
            PhysicalId = dto.PhysicalId,
            Location = dto.Location,
            SamplingFrequency = dto.SamplingFrequency,
            Status = dto.Status
        };
    }
}
