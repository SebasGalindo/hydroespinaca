using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AuthService.Infrastructure.Persistence.Mappers;
using AuthService.Infrastructure.Persistence.Schemas;
using HydroEspinaca.Shared.Mongo;
using MongoDB.Driver;

namespace AuthService.Infrastructure.Persistence.Repositories;

/// <summary>
/// MongoDB implementation of password reset token repository using shared BaseMongoRepository
/// </summary>
public class MongoPasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly BaseMongoRepository<PasswordResetToken, PasswordResetTokenDocument> _baseRepo;
    private readonly IMongoCollection<PasswordResetTokenDocument> _collection;

    public MongoPasswordResetTokenRepository(IMongoDatabase database)
    {
        _baseRepo = new BaseMongoRepository<PasswordResetToken, PasswordResetTokenDocument>(
            database,
            "passwordResetTokens",
            new PasswordResetTokenMapper()
        );
        
        _collection = database.GetCollection<PasswordResetTokenDocument>("passwordResetTokens");
        
        // Create indexes for better performance
        CreateIndexes();
    }

    /// <summary>
    /// Creates a new password reset token
    /// </summary>
    /// <param name="token">Password reset token to create</param>
    public async Task CreateAsync(PasswordResetToken token)
    {
        await _baseRepo.CreateAsync(token);
    }

    /// <summary>
    /// Gets an active (non-used, non-expired) password reset token for a user
    /// </summary>
    /// <param name="userId">User ID to search for</param>
    /// <returns>Active password reset token or null if none found</returns>
    public async Task<PasswordResetToken?> GetActiveByUserIdAsync(string userId)
    {
        var now = DateTime.UtcNow;
        var filter = Builders<PasswordResetTokenDocument>.Filter.And(
            Builders<PasswordResetTokenDocument>.Filter.Eq(x => x.UserId, userId),
            Builders<PasswordResetTokenDocument>.Filter.Eq(x => x.IsUsed, false),
            Builders<PasswordResetTokenDocument>.Filter.Gt(x => x.ExpiresAt, now)
        );

        return await _baseRepo.FindOneAsync(filter);
    }

    /// <summary>
    /// Gets a password reset token by its ID
    /// </summary>
    /// <param name="id">Token ID</param>
    /// <returns>Password reset token or null if not found</returns>
    public async Task<PasswordResetToken?> GetByIdAsync(string id)
    {
        var filter = Builders<PasswordResetTokenDocument>.Filter.Eq(x => x.Id, id);
        return await _baseRepo.FindOneAsync(filter);
    }

    /// <summary>
    /// Gets a password reset token by user ID and code
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="code">Reset code</param>
    /// <returns>Password reset token or null if not found</returns>
    public async Task<PasswordResetToken?> GetByUserIdAndCodeAsync(string userId, string code)
    {
        var filter = Builders<PasswordResetTokenDocument>.Filter.And(
            Builders<PasswordResetTokenDocument>.Filter.Eq(x => x.UserId, userId),
            Builders<PasswordResetTokenDocument>.Filter.Eq(x => x.Code, code)
        );

        return await _baseRepo.FindOneAsync(filter);
    }

    /// <summary>
    /// Updates an existing password reset token
    /// </summary>
    /// <param name="token">Token to update</param>
    public async Task UpdateAsync(PasswordResetToken token)
    {
        await _baseRepo.UpdateAsync(token);
    }

    /// <summary>
    /// Deletes expired password reset tokens
    /// </summary>
    /// <returns>Number of deleted tokens</returns>
    public async Task<int> DeleteExpiredAsync()
    {
        var now = DateTime.UtcNow;
        var filter = Builders<PasswordResetTokenDocument>.Filter.Lt(x => x.ExpiresAt, now);
        
        var result = await _collection.DeleteManyAsync(filter);
        return (int)result.DeletedCount;
    }

    /// <summary>
    /// Revokes (marks as used) all active tokens for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Number of revoked tokens</returns>
    public async Task<int> RevokeActiveTokensForUserAsync(string userId)
    {
        var now = DateTime.UtcNow;
        var filter = Builders<PasswordResetTokenDocument>.Filter.And(
            Builders<PasswordResetTokenDocument>.Filter.Eq(x => x.UserId, userId),
            Builders<PasswordResetTokenDocument>.Filter.Eq(x => x.IsUsed, false),
            Builders<PasswordResetTokenDocument>.Filter.Gt(x => x.ExpiresAt, now)
        );

        var update = Builders<PasswordResetTokenDocument>.Update.Set(x => x.IsUsed, true);
        var result = await _collection.UpdateManyAsync(filter, update);
        
        return (int)result.ModifiedCount;
    }

    /// <summary>
    /// Creates database indexes for better performance
    /// </summary>
    private void CreateIndexes()
    {
        var indexKeysDefinition = Builders<PasswordResetTokenDocument>.IndexKeys
            .Ascending(x => x.UserId)
            .Ascending(x => x.IsUsed)
            .Ascending(x => x.ExpiresAt);

        var indexModel = new CreateIndexModel<PasswordResetTokenDocument>(indexKeysDefinition);
        _collection.Indexes.CreateOne(indexModel);

        // Index for cleanup of expired tokens
        var expiredIndexKeys = Builders<PasswordResetTokenDocument>.IndexKeys.Ascending(x => x.ExpiresAt);
        var expiredIndexModel = new CreateIndexModel<PasswordResetTokenDocument>(expiredIndexKeys);
        _collection.Indexes.CreateOne(expiredIndexModel);

        // Index for user-code lookup
        var userCodeIndexKeys = Builders<PasswordResetTokenDocument>.IndexKeys
            .Ascending(x => x.UserId)
            .Ascending(x => x.Code);
        var userCodeIndexModel = new CreateIndexModel<PasswordResetTokenDocument>(userCodeIndexKeys);
        _collection.Indexes.CreateOne(userCodeIndexModel);
    }
}