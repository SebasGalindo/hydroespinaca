namespace ActuatorService.Domain.Interfaces;

/// <summary>
/// Manages pin locking to prevent concurrent access to the same actuator pins.
/// Thread-safe registry for tracking which pins are in use by active routines.
/// </summary>
public interface IPinLockRegistry
{
    /// <summary>
    /// Attempts to lock a set of pins for a routine.
    /// </summary>
    /// <param name="pins">Pins to lock</param>
    /// <param name="routineId">ID of the routine requesting the lock</param>
    /// <returns>True if all pins were successfully locked, false if any pin is already locked</returns>
    bool TryLock(IEnumerable<string> pins, string routineId);

    /// <summary>
    /// Releases the locks on a set of pins.
    /// </summary>
    /// <param name="pins">Pins to release</param>
    void Release(IEnumerable<string> pins);

    /// <summary>
    /// Checks if a specific pin is currently locked.
    /// </summary>
    /// <param name="pin">Pin to check</param>
    /// <returns>True if the pin is locked, false otherwise</returns>
    bool IsLocked(string pin);

    /// <summary>
    /// Gets the routine ID that currently holds the lock for a pin.
    /// </summary>
    /// <param name="pin">Pin to query</param>
    /// <returns>RoutineId holding the lock, or null if not locked</returns>
    string? GetLockHolder(string pin);

    /// <summary>
    /// Gets all currently locked pins.
    /// </summary>
    IReadOnlyDictionary<string, string> GetAllLocks();
}
