using Microsoft.AspNetCore.Mvc;
using SensorService.Api.Dtos;
using SensorService.Domain.Entities;
using SensorService.Domain.Interfaces;
using SensorService.Api.Mappers;

namespace SensorService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SensorsController : ControllerBase
{
    private readonly ISensorRepository _repository;

    public SensorsController(ISensorRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Sensor>>> GetAll()
    {
        var sensors = await _repository.GetAllAsync();
        return Ok(sensors);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Sensor>> Get(string id)
    {
        var sensor = await _repository.GetByIdAsync(id);
        return sensor is null ? NotFound() : Ok(sensor);
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] CreateSensorRequest request)
    {
        var sensor = request.ToDomain();
        await _repository.AddAsync(sensor);
        return Created($"/api/sensors/{sensor.Id}", sensor);

    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(string id, Sensor sensor)
    {
        if (id != sensor.Id)
            return BadRequest("ID mismatch");

        await _repository.UpdateAsync(sensor);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(string id)
    {
        await _repository.DeleteAsync(id);
        return NoContent();
    }
}
