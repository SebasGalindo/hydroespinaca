using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AuthService.Infrastructure.Persistence.Mappers;
using AuthService.Infrastructure.Persistence.Schemas;
using HydroEspinaca.Shared.Mongo;
using MongoDB.Driver;

namespace AuthService.Infrastructure.Persistence.Repositories;

/// <summary>
/// MongoDB repository implementation for user session entities.
/// </summary>
public class MongoUserSessionRepository : IUserSessionRepository
{
    private readonly BaseMongoRepository<UserSession, UserSessionDocument> _baseRepo;

    public MongoUserSessionRepository(IMongoDatabase db)
    {
        _baseRepo = new BaseMongoRepository<UserSession, UserSessionDocument>(
            db,
            "user_sessions",
            new UserSessionMapper()
        );
    }

    public async Task<UserSession?> FindBySessionIdAsync(string sessionId)
    {
        var filter = Builders<UserSessionDocument>.Filter.Eq(x => x.SessionId, sessionId);
        return await _baseRepo.FindOneAsync(filter);
    }

    public async Task<UserSession?> FindByRefreshTokenAsync(string refreshToken)
    {
        var filter = Builders<UserSessionDocument>.Filter.Eq(x => x.RefreshToken, refreshToken);
        return await _baseRepo.FindOneAsync(filter);
    }

    public async Task<IEnumerable<UserSession>> FindActiveByUserIdAsync(string userId)
    {
        var filter = Builders<UserSessionDocument>.Filter.And(
            Builders<UserSessionDocument>.Filter.Eq(x => x.UserId, userId),
            Builders<UserSessionDocument>.Filter.Eq(x => x.Revoked, false),
            Builders<UserSessionDocument>.Filter.Gt(x => x.ExpiresAt, DateTime.UtcNow)
        );
        return await _baseRepo.FindManyAsync(filter);
    }

    public async Task<Dictionary<string, List<UserSession>>> GetAllActiveSessionsGroupedByUserAsync()
    {
        var filter = Builders<UserSessionDocument>.Filter.And(
            Builders<UserSessionDocument>.Filter.Eq(x => x.Revoked, false),
            Builders<UserSessionDocument>.Filter.Gt(x => x.ExpiresAt, DateTime.UtcNow)
        );
        
        var sort = Builders<UserSessionDocument>.Sort.Descending(x => x.LastActivity);
        var sessions = await _baseRepo.FindManyAsync(filter, sort);
        
        return sessions
            .GroupBy(s => s.UserId)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    public Task AddAsync(UserSession session)
        => _baseRepo.CreateAsync(session);

    public Task UpdateAsync(UserSession session)
        => _baseRepo.UpdateAsync(session);
}
