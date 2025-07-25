using HydroEspinaca.Shared.Mongo;
using HydroEspinaca.Shared.Mongo.Interfaces;
using SensorService.Domain.Exceptions;
using SensorService.Domain.Interfaces;
using SensorService.Infrastructure.Persistence.Models;

namespace SensorService.Infrastructure.Persistence.Repositories;

public class MongoVariableRepository : IVariableRepository
{
    private readonly BaseMongoRepository<Variable, VariableDocument> _baseRepo;

    public MongoVariableRepository(MongoDbContext ctx, IEntityMapper<Variable, VariableDocument> mapper)
    {
        _baseRepo = new BaseMongoRepository<Variable, VariableDocument>(ctx.Database, "variables", mapper);
    }

    public async Task<Variable?> GetByIdAsync(string id)
    {
        try
        {
            return await _baseRepo.GetByIdAsync(id);
        }
        catch (Exception ex)
        {
            throw new DatabaseOperationException("Error retrieving variable by ID", ex);
        }
    }

    public async Task<List<Variable>> GetAllAsync()
    {
        try
        {
            return await _baseRepo.GetAllAsync();
        }
        catch (Exception ex)
        {
            throw new DatabaseOperationException("Error retrieving all variables", ex);
        }
    }

    public async Task CreateAsync(Variable variable)
    {
        try
        {
            await _baseRepo.CreateAsync(variable);
        }
        catch (Exception ex)
        {
            throw new DatabaseOperationException("Error creating variable", ex);
        }
    }

    public async Task UpdateAsync(Variable variable)
    {
        try
        {
            await _baseRepo.UpdateAsync(variable);
        }
        catch (Exception ex)
        {
            throw new DatabaseOperationException("Error updating variable", ex);
        }
    }

    public async Task DeleteAsync(string id)
    {
        try
        {
            await _baseRepo.DeleteAsync(id);
        }
        catch (Exception ex)
        {
            throw new DatabaseOperationException("Error deleting variable", ex);
        }
    }

    public async Task<int> CountByIdsAsync(IEnumerable<string> ids)
    {
        try
        {
            return (int)await _baseRepo.CountAsync(x => ids.Contains(x.Id));
        }
        catch (Exception ex)
        {
            throw new DatabaseOperationException("Error counting variables by IDs", ex);
        }
    }

}
