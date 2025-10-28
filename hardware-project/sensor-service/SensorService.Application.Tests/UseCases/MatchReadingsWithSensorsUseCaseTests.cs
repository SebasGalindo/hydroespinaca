using Microsoft.Extensions.Logging;
using HydroEspinaca.Shared.DTOs.Mqtt;
using SensorService.Application.UseCases.ProcessReadingBatch;
using SensorService.Domain.Entities;
using SensorService.Domain.Interfaces;
using SensorService.Application.Tests.Builders;

namespace SensorService.Application.Tests.UseCases;

public class MatchReadingsWithSensorsUseCaseTests
{
    private readonly Mock<ISensorRepository> _sensorRepositoryMock;
    private readonly Mock<ILogger<MatchReadingsWithSensorsUseCase>> _loggerMock;
    private readonly MatchReadingsWithSensorsUseCase _sut;

    public MatchReadingsWithSensorsUseCaseTests()
    {
        _sensorRepositoryMock = new Mock<ISensorRepository>();
        _loggerMock = new Mock<ILogger<MatchReadingsWithSensorsUseCase>>();
        _sut = new MatchReadingsWithSensorsUseCase(_sensorRepositoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WithMatchingReadings_ReturnsMatchedReadings()
    {
        // Arrange
        var sensors = new List<Sensor>
        {
            new SensorBuilder()
                .WithCode("DHT22-01")
                .WithPhysicalId("DHT22-A1")
                .WithVariables(new List<string> { "T_AMB", "HUM" })
                .Build()
        };

        var dto = new ReadingBatchDto
        {
            Esp32Id = "esp32-01",
            Timestamp = DateTimeOffset.UtcNow,
            Readings = new List<ReadingInput>
            {
                new ReadingInput
                {
                    PhysicalId = "DHT22-A1",
                    VariableCode = "T_AMB",
                    Value = 22.5
                }
            }
        };

        _sensorRepositoryMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(sensors);

        // Act
        var result = await _sut.ExecuteAsync(dto);

        // Assert
        var matchedReadings = result.ToList();
        matchedReadings.Should().HaveCount(1);
        matchedReadings[0].SensorCode.Should().Be("DHT22-01");
        matchedReadings[0].VariableCode.Should().Be("T_AMB");
        matchedReadings[0].Value.Should().Be(22.5);
        matchedReadings[0].Timestamp.Should().BeCloseTo(dto.Timestamp.UtcDateTime, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task ExecuteAsync_WithNonMatchingPhysicalId_ReturnsEmpty()
    {
        // Arrange
        var sensors = new List<Sensor>
        {
            new SensorBuilder()
                .WithCode("DHT22-01")
                .WithPhysicalId("DHT22-A1")
                .WithVariables(new List<string> { "T_AMB", "HUM" })
                .Build()
        };

        var dto = new ReadingBatchDto
        {
            Esp32Id = "esp32-01",
            Timestamp = DateTimeOffset.UtcNow,
            Readings = new List<ReadingInput>
            {
                new ReadingInput
                {
                    PhysicalId = "UNKNOWN-SENSOR", // No match
                    VariableCode = "T_AMB",
                    Value = 22.5
                }
            }
        };

        _sensorRepositoryMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(sensors);

        // Act
        var result = await _sut.ExecuteAsync(dto);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_WithNonMatchingVariableCode_ReturnsEmpty()
    {
        // Arrange
        var sensors = new List<Sensor>
        {
            new SensorBuilder()
                .WithCode("DHT22-01")
                .WithPhysicalId("DHT22-A1")
                .WithVariables(new List<string> { "T_AMB", "HUM" })
                .Build()
        };

        var dto = new ReadingBatchDto
        {
            Esp32Id = "esp32-01",
            Timestamp = DateTimeOffset.UtcNow,
            Readings = new List<ReadingInput>
            {
                new ReadingInput
                {
                    PhysicalId = "DHT22-A1",
                    VariableCode = "UNKNOWN_VAR", // No match
                    Value = 22.5
                }
            }
        };

        _sensorRepositoryMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(sensors);

        // Act
        var result = await _sut.ExecuteAsync(dto);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_WithMultipleReadings_ReturnsOnlyMatched()
    {
        // Arrange
        var sensors = new List<Sensor>
        {
            new SensorBuilder()
                .WithCode("DHT22-01")
                .WithPhysicalId("DHT22-A1")
                .WithVariables(new List<string> { "T_AMB", "HUM" })
                .Build(),
            new SensorBuilder()
                .WithCode("PH-01")
                .WithPhysicalId("PH-SENSOR-1")
                .WithVariables(new List<string> { "PH" })
                .Build()
        };

        var dto = new ReadingBatchDto
        {
            Esp32Id = "esp32-01",
            Timestamp = DateTimeOffset.UtcNow,
            Readings = new List<ReadingInput>
            {
                new ReadingInput { PhysicalId = "DHT22-A1", VariableCode = "T_AMB", Value = 22.5 },
                new ReadingInput { PhysicalId = "DHT22-A1", VariableCode = "HUM", Value = 65.0 },
                new ReadingInput { PhysicalId = "UNKNOWN", VariableCode = "EC", Value = 1.8 }, // No match
                new ReadingInput { PhysicalId = "PH-SENSOR-1", VariableCode = "PH", Value = 6.5 }
            }
        };

        _sensorRepositoryMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(sensors);

        // Act
        var result = await _sut.ExecuteAsync(dto);

        // Assert
        var matchedReadings = result.ToList();
        matchedReadings.Should().HaveCount(3);
        matchedReadings.Should().Contain(r => r.SensorCode == "DHT22-01" && r.VariableCode == "T_AMB");
        matchedReadings.Should().Contain(r => r.SensorCode == "DHT22-01" && r.VariableCode == "HUM");
        matchedReadings.Should().Contain(r => r.SensorCode == "PH-01" && r.VariableCode == "PH");
    }

    [Fact]
    public async Task ExecuteAsync_WithNoSensors_ReturnsEmpty()
    {
        // Arrange
        var dto = new ReadingBatchDto
        {
            Esp32Id = "esp32-01",
            Timestamp = DateTimeOffset.UtcNow,
            Readings = new List<ReadingInput>
            {
                new ReadingInput { PhysicalId = "DHT22-A1", VariableCode = "T_AMB", Value = 22.5 }
            }
        };

        _sensorRepositoryMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Sensor>());

        // Act
        var result = await _sut.ExecuteAsync(dto);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyReadings_ReturnsEmpty()
    {
        // Arrange
        var sensors = new List<Sensor>
        {
            new SensorBuilder()
                .WithCode("DHT22-01")
                .WithPhysicalId("DHT22-A1")
                .WithVariables(new List<string> { "T_AMB" })
                .Build()
        };

        var dto = new ReadingBatchDto
        {
            Esp32Id = "esp32-01",
            Timestamp = DateTimeOffset.UtcNow,
            Readings = new List<ReadingInput>()
        };

        _sensorRepositoryMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(sensors);

        // Act
        var result = await _sut.ExecuteAsync(dto);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_LogsCorrectInformation()
    {
        // Arrange
        var sensors = new List<Sensor>
        {
            new SensorBuilder()
                .WithCode("DHT22-01")
                .WithPhysicalId("DHT22-A1")
                .WithVariables(new List<string> { "T_AMB" })
                .Build()
        };

        var dto = new ReadingBatchDto
        {
            Esp32Id = "esp32-01",
            Timestamp = DateTimeOffset.UtcNow,
            Readings = new List<ReadingInput>
            {
                new ReadingInput { PhysicalId = "DHT22-A1", VariableCode = "T_AMB", Value = 22.5 }
            }
        };

        _sensorRepositoryMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(sensors);

        // Act
        await _sut.ExecuteAsync(dto);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("matched readings")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.AtLeastOnce
        );
    }
}
