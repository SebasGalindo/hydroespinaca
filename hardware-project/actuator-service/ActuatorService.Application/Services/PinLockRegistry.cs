using System.Collections.Concurrent;
using ActuatorService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ActuatorService.Application.Services;

/// <summary>
/// Thread-safe registry for managing pin locks across concurrent routine executions.
/// Prevents two routines from using the same actuator pin simultaneously.
/// </summary>
public class PinLockRegistry : IPinLockRegistry
{
    private readonly ConcurrentDictionary<string, string> _pinLocks = new();
    private readonly ILogger<PinLockRegistry> _logger;

    public PinLockRegistry(ILogger<PinLockRegistry> logger)
    {
        _logger = logger;
    }

    public bool TryLock(IEnumerable<string> pins, string routineId)
    {
        var pinList = pins.ToList();

        // First check if ALL pins are available
        var lockedPins = pinList.Where(IsLocked).ToList();
        if (lockedPins.Any())
        {
            _logger.LogDebug("🔒 Cannot lock pins for routine {RoutineId}: {LockedPins} already locked by {Holders}",
                routineId,
                string.Join(", ", lockedPins),
                string.Join(", ", lockedPins.Select(GetLockHolder)));
            return false;
        }

        // All pins available - acquire locks atomically
        var acquired = new List<string>();
        try
        {
            foreach (var pin in pinList)
            {
                if (!_pinLocks.TryAdd(pin, routineId))
                {
                    // Rollback - this shouldn't happen due to pre-check, but be safe
                    _logger.LogWarning("⚠️ Failed to acquire lock for pin {Pin} during atomic lock for routine {RoutineId}",
                        pin, routineId);

                    // Rollback acquired locks
                    foreach (var acquiredPin in acquired)
                    {
                        _pinLocks.TryRemove(acquiredPin, out _);
                    }
                    return false;
                }
                acquired.Add(pin);
            }

            _logger.LogInformation("🔐 Locked pins {Pins} for routine {RoutineId}",
                string.Join(", ", pinList), routineId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error acquiring pin locks for routine {RoutineId}", routineId);

            // Rollback on error
            foreach (var pin in acquired)
            {
                _pinLocks.TryRemove(pin, out _);
            }
            return false;
        }
    }

    public void Release(IEnumerable<string> pins)
    {
        var pinList = pins.ToList();
        var released = new List<string>();

        foreach (var pin in pinList)
        {
            if (_pinLocks.TryRemove(pin, out var routineId))
            {
                released.Add(pin);
                _logger.LogDebug("🔓 Released lock on pin {Pin} (was held by routine {RoutineId})",
                    pin, routineId);
            }
        }

        if (released.Any())
        {
            _logger.LogInformation("🔓 Released {Count} pin locks: {Pins}",
                released.Count, string.Join(", ", released));
        }
    }

    public bool IsLocked(string pin)
    {
        return _pinLocks.ContainsKey(pin);
    }

    public string? GetLockHolder(string pin)
    {
        return _pinLocks.TryGetValue(pin, out var routineId) ? routineId : null;
    }

    public IReadOnlyDictionary<string, string> GetAllLocks()
    {
        return _pinLocks.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }
}
