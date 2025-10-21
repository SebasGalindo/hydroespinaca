using ActuatorService.Domain.Entities;

namespace ActuatorService.Domain.Interfaces;

/// <summary>
/// Repository for managing internal recurring routines
/// </summary>
public interface IInternalRoutineRepository
{
    /// <summary>
    /// Gets all active internal routines
    /// </summary>
    Task<List<InternalRoutine>> GetActiveRoutinesAsync();

    /// <summary>
    /// Adds a new internal routine
    /// </summary>
    Task AddAsync(InternalRoutine routine);

    /// <summary>
    /// Gets a routine by ID
    /// </summary>
    Task<InternalRoutine?> GetByIdAsync(string id);

    /// <summary>
    /// Gets a routine by name
    /// </summary>
    Task<InternalRoutine?> GetByNameAsync(string name);

    /// <summary>
    /// Updates a routine
    /// </summary>
    Task UpdateAsync(InternalRoutine routine);

    /// <summary>
    /// Deletes a routine by ID
    /// </summary>
    Task DeleteAsync(string id);
}
