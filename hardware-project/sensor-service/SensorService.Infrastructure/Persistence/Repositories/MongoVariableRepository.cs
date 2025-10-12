using HydroEspinaca.Shared.Enums;
using HydroEspinaca.Shared.Mongo;
using HydroEspinaca.Shared.Mongo.Interfaces;
using MongoDB.Driver;
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
        return await _baseRepo.GetByIdAsync(id);
    }

    public async Task<Variable?> GetByCodeAsync(string code)
    {
        var filter = Builders<VariableDocument>.Filter.Eq(v => v.Code, code);
        return await _baseRepo.FindOneAsync(filter);
    }

    public async Task<List<Variable>> GetAllAsync()
    {
        return await _baseRepo.GetAllAsync();
    }

    public async Task<List<Variable>> GetByRegulationTypeAsync(RegulationType regulationType, CancellationToken cancellationToken = default)
    {
        var filter = Builders<VariableDocument>.Filter.Eq(v => v.RegulationType, regulationType);
        return await _baseRepo.FindManyAsync(filter);
    }

    public async Task CreateAsync(Variable variable)
    {
        await _baseRepo.CreateAsync(variable);
    }

    public async Task UpdateAsync(Variable variable)
    {
        await _baseRepo.UpdateAsync(variable);
    }

    public async Task DeleteAsync(string id)
    {
        await _baseRepo.DeleteAsync(id);
    }

    public async Task<List<string>> GetNonExistingIdsAsync(IEnumerable<string> ids)
    {
        return await _baseRepo.GetNonExistingIdsAsync(ids);
    }

    public async Task<List<string>> GetNonExistingCodesAsync(IEnumerable<string> codes)
    {
        var codeList = codes.ToList();
        if (!codeList.Any()) return new List<string>();

        var filter = Builders<VariableDocument>.Filter.In(v => v.Code, codeList);
        var existingVariables = await _baseRepo.FindManyAsync(filter);
        var existingCodes = existingVariables.Select(v => v.Code).ToHashSet();

        return codeList.Where(code => !existingCodes.Contains(code)).ToList();
    }

}
