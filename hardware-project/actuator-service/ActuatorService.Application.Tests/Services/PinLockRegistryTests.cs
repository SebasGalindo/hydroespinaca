using ActuatorService.Application.Services;
using Microsoft.Extensions.Logging;

namespace ActuatorService.Application.Tests.Services;

/// <summary>
/// Tests unitarios para PinLockRegistry
/// Cobertura: gestión de bloqueos de pines, concurrencia, y prevención de conflictos
/// </summary>
public class PinLockRegistryTests
{
    private readonly PinLockRegistry _pinLockRegistry;
    private readonly Mock<ILogger<PinLockRegistry>> _mockLogger;

    public PinLockRegistryTests()
    {
        _mockLogger = new Mock<ILogger<PinLockRegistry>>();
        _pinLockRegistry = new PinLockRegistry(_mockLogger.Object);
    }

    [Fact]
    public void TryLock_SinglePin_WhenAvailable_ShouldSucceed()
    {
        // Arrange
        var pins = new List<string> { "GPIO_5" };
        const string routineId = "routine-001";

        // Act
        var result = _pinLockRegistry.TryLock(pins, routineId);

        // Assert
        result.Should().BeTrue();
        _pinLockRegistry.IsLocked("GPIO_5").Should().BeTrue();
        _pinLockRegistry.GetLockHolder("GPIO_5").Should().Be(routineId);
    }

    [Fact]
    public void TryLock_MultiplePins_WhenAllAvailable_ShouldSucceed()
    {
        // Arrange
        var pins = new List<string> { "GPIO_5", "GPIO_10", "GPIO_15" };
        const string routineId = "routine-001";

        // Act
        var result = _pinLockRegistry.TryLock(pins, routineId);

        // Assert
        result.Should().BeTrue();
        pins.Should().OnlyContain(pin => _pinLockRegistry.IsLocked(pin));
        pins.Should().OnlyContain(pin => _pinLockRegistry.GetLockHolder(pin) == routineId);
    }

    [Fact]
    public void TryLock_WhenPinAlreadyLocked_ShouldFail()
    {
        // Arrange
        var pins = new List<string> { "GPIO_5" };
        _pinLockRegistry.TryLock(pins, "routine-001");

        // Act - try to lock the same pin with different routine
        var result = _pinLockRegistry.TryLock(pins, "routine-002");

        // Assert
        result.Should().BeFalse();
        _pinLockRegistry.GetLockHolder("GPIO_5").Should().Be("routine-001");
    }

    [Fact]
    public void TryLock_MultiplePins_WhenOneIsLocked_ShouldFailAndNotLockAny()
    {
        // Arrange
        _pinLockRegistry.TryLock(new List<string> { "GPIO_10" }, "routine-001");
        var pins = new List<string> { "GPIO_5", "GPIO_10", "GPIO_15" };

        // Act
        var result = _pinLockRegistry.TryLock(pins, "routine-002");

        // Assert
        result.Should().BeFalse();
        _pinLockRegistry.IsLocked("GPIO_5").Should().BeFalse();
        _pinLockRegistry.IsLocked("GPIO_10").Should().BeTrue();
        _pinLockRegistry.GetLockHolder("GPIO_10").Should().Be("routine-001");
        _pinLockRegistry.IsLocked("GPIO_15").Should().BeFalse();
    }

    [Fact]
    public void Release_SinglePin_ShouldUnlockPin()
    {
        // Arrange
        var pins = new List<string> { "GPIO_5" };
        _pinLockRegistry.TryLock(pins, "routine-001");

        // Act
        _pinLockRegistry.Release(pins);

        // Assert
        _pinLockRegistry.IsLocked("GPIO_5").Should().BeFalse();
        _pinLockRegistry.GetLockHolder("GPIO_5").Should().BeNull();
    }

    [Fact]
    public void Release_MultiplePins_ShouldUnlockAllPins()
    {
        // Arrange
        var pins = new List<string> { "GPIO_5", "GPIO_10", "GPIO_15" };
        _pinLockRegistry.TryLock(pins, "routine-001");

        // Act
        _pinLockRegistry.Release(pins);

        // Assert
        pins.Should().OnlyContain(pin => !_pinLockRegistry.IsLocked(pin));
        pins.Should().OnlyContain(pin => _pinLockRegistry.GetLockHolder(pin) == null);
    }

    [Fact]
    public void Release_NonLockedPin_ShouldNotThrowException()
    {
        // Arrange
        var pins = new List<string> { "GPIO_5" };

        // Act
        var action = () => _pinLockRegistry.Release(pins);

        // Assert
        action.Should().NotThrow();
    }

    [Fact]
    public void IsLocked_WhenPinNotLocked_ShouldReturnFalse()
    {
        // Act
        var result = _pinLockRegistry.IsLocked("GPIO_5");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetLockHolder_WhenPinNotLocked_ShouldReturnNull()
    {
        // Act
        var holder = _pinLockRegistry.GetLockHolder("GPIO_5");

        // Assert
        holder.Should().BeNull();
    }

    [Fact]
    public void GetAllLocks_ShouldReturnAllLockedPins()
    {
        // Arrange
        _pinLockRegistry.TryLock(new List<string> { "GPIO_5" }, "routine-001");
        _pinLockRegistry.TryLock(new List<string> { "GPIO_10", "GPIO_15" }, "routine-002");

        // Act
        var allLocks = _pinLockRegistry.GetAllLocks();

        // Assert
        allLocks.Should().HaveCount(3);
        allLocks.Should().ContainKey("GPIO_5").WhoseValue.Should().Be("routine-001");
        allLocks.Should().ContainKey("GPIO_10").WhoseValue.Should().Be("routine-002");
        allLocks.Should().ContainKey("GPIO_15").WhoseValue.Should().Be("routine-002");
    }

    [Fact]
    public void GetAllLocks_WhenNoLocks_ShouldReturnEmptyDictionary()
    {
        // Act
        var allLocks = _pinLockRegistry.GetAllLocks();

        // Assert
        allLocks.Should().BeEmpty();
    }

    [Fact]
    public void TryLock_SamePinDifferentRoutines_ShouldMaintainFirstLock()
    {
        // Arrange
        var pins = new List<string> { "GPIO_5" };

        // Act
        var result1 = _pinLockRegistry.TryLock(pins, "routine-001");
        var result2 = _pinLockRegistry.TryLock(pins, "routine-002");
        var result3 = _pinLockRegistry.TryLock(pins, "routine-003");

        // Assert
        result1.Should().BeTrue();
        result2.Should().BeFalse();
        result3.Should().BeFalse();
        _pinLockRegistry.GetLockHolder("GPIO_5").Should().Be("routine-001");
    }

    [Fact]
    public void LockReleaseSequence_ShouldAllowReusability()
    {
        // Arrange
        var pins = new List<string> { "GPIO_5" };

        // Act & Assert - First lock
        _pinLockRegistry.TryLock(pins, "routine-001").Should().BeTrue();
        _pinLockRegistry.IsLocked("GPIO_5").Should().BeTrue();

        // Release
        _pinLockRegistry.Release(pins);
        _pinLockRegistry.IsLocked("GPIO_5").Should().BeFalse();

        // Second lock (different routine)
        _pinLockRegistry.TryLock(pins, "routine-002").Should().BeTrue();
        _pinLockRegistry.GetLockHolder("GPIO_5").Should().Be("routine-002");

        // Release again
        _pinLockRegistry.Release(pins);
        _pinLockRegistry.IsLocked("GPIO_5").Should().BeFalse();
    }

    [Fact]
    public async Task ConcurrentLockAttempts_ShouldBeThreadSafe()
    {
        // Arrange
        const string pin = "GPIO_5";
        const int threadCount = 10;
        var successCount = 0;
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < threadCount; i++)
        {
            var routineId = $"routine-{i:D3}";
            tasks.Add(Task.Run(() =>
            {
                if (_pinLockRegistry.TryLock(new List<string> { pin }, routineId))
                {
                    Interlocked.Increment(ref successCount);
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        successCount.Should().Be(1, "only one thread should successfully acquire the lock");
        _pinLockRegistry.IsLocked(pin).Should().BeTrue();
        _pinLockRegistry.GetLockHolder(pin).Should().MatchRegex(@"^routine-\d{3}$");
    }

    [Fact]
    public async Task ConcurrentLockAndRelease_ShouldMaintainConsistency()
    {
        // Arrange
        const string pin = "GPIO_10";
        const int operationCount = 50;
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < operationCount; i++)
        {
            var routineId = $"routine-{i:D3}";
            tasks.Add(Task.Run(async () =>
            {
                if (_pinLockRegistry.TryLock(new List<string> { pin }, routineId))
                {
                    // Simulate some work
                    await Task.Delay(10);
                    _pinLockRegistry.Release(new List<string> { pin });
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        _pinLockRegistry.IsLocked(pin).Should().BeFalse("all locks should be released");
    }

    [Fact]
    public void TryLock_EmptyPinList_ShouldReturnTrue()
    {
        // Arrange
        var pins = new List<string>();

        // Act
        var result = _pinLockRegistry.TryLock(pins, "routine-001");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Release_EmptyPinList_ShouldNotThrow()
    {
        // Arrange
        var pins = new List<string>();

        // Act
        var action = () => _pinLockRegistry.Release(pins);

        // Assert
        action.Should().NotThrow();
    }

    [Theory]
    [InlineData("GPIO_5")]
    [InlineData("GPIO_10")]
    [InlineData("GPIO_15")]
    [InlineData("GPIO_20")]
    public void TryLock_DifferentPins_ShouldAllSucceed(string pin)
    {
        // Arrange
        var pins = new List<string> { pin };
        var routineId = $"routine-for-{pin}";

        // Act
        var result = _pinLockRegistry.TryLock(pins, routineId);

        // Assert
        result.Should().BeTrue();
        _pinLockRegistry.IsLocked(pin).Should().BeTrue();
        _pinLockRegistry.GetLockHolder(pin).Should().Be(routineId);
    }

    [Fact]
    public void ComplexScenario_MultipleRoutinesMultiplePins_ShouldWorkCorrectly()
    {
        // Arrange & Act
        var result1 = _pinLockRegistry.TryLock(new List<string> { "GPIO_5", "GPIO_10" }, "routine-001");
        var result2 = _pinLockRegistry.TryLock(new List<string> { "GPIO_15" }, "routine-002");
        var result3 = _pinLockRegistry.TryLock(new List<string> { "GPIO_10", "GPIO_20" }, "routine-003"); // Should fail (GPIO_10 locked)
        
        _pinLockRegistry.Release(new List<string> { "GPIO_5" });
        
        var result4 = _pinLockRegistry.TryLock(new List<string> { "GPIO_5", "GPIO_20" }, "routine-004");

        // Assert
        result1.Should().BeTrue();
        result2.Should().BeTrue();
        result3.Should().BeFalse();
        result4.Should().BeTrue();

        _pinLockRegistry.IsLocked("GPIO_5").Should().BeTrue();
        _pinLockRegistry.GetLockHolder("GPIO_5").Should().Be("routine-004");
        _pinLockRegistry.IsLocked("GPIO_10").Should().BeTrue();
        _pinLockRegistry.GetLockHolder("GPIO_10").Should().Be("routine-001");
        _pinLockRegistry.IsLocked("GPIO_15").Should().BeTrue();
        _pinLockRegistry.GetLockHolder("GPIO_15").Should().Be("routine-002");
        _pinLockRegistry.IsLocked("GPIO_20").Should().BeTrue();
        _pinLockRegistry.GetLockHolder("GPIO_20").Should().Be("routine-004");
    }
}
