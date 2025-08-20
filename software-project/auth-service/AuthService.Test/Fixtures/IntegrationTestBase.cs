using AuthService.Api;
using AuthService.Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;
using Testcontainers.MongoDb;

namespace AuthService.Test.Fixtures;

public class IntegrationTestBase : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MongoDbContainer _mongoContainer;
    private readonly string _databaseName = $"auth_test_{Guid.NewGuid():N}";

    public IntegrationTestBase()
    {
        _mongoContainer = new MongoDbBuilder()
            .WithImage("mongo:6.0")
            .WithPortBinding(27017, true)
            .Build();
    }

    public IMongoDatabase Database { get; private set; } = null!;
    public string ConnectionString => _mongoContainer.GetConnectionString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Mongo:ConnectionString"] = ConnectionString,
                ["Mongo:Database"] = _databaseName,
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience",
                ["Jwt:AccessTokenExpiryMinutes"] = "30",
                ["Jwt:RefreshTokenExpiryDays"] = "1",
                // Not setting PrivateKeyPath/PublicKeyPath so it uses InMemoryTestKeyStore
            });
        });

        builder.ConfigureServices(services =>
        {
            // DataSeedingService ahora se registra correctamente en AddInfrastructureServices
            // No necesitamos removerlo porque ahora no falla el registro
        });

        builder.UseEnvironment("Test");
    }



    public async Task InitializeAsync()
    {
        await _mongoContainer.StartAsync();

        var mongoClient = new MongoClient(ConnectionString);
        Database = mongoClient.GetDatabase(_databaseName);
        
        // Note: DataSeedingService will be called by AuthTestHelper when needed
        // This provides better control over when seeding happens per test
    }

    public new async Task DisposeAsync()
    {
        await _mongoContainer.StopAsync();
        await _mongoContainer.DisposeAsync();
        await base.DisposeAsync();
    }

    public async Task CleanupDatabaseAsync()
    {
        // Drop all collections to clean state
        var collections = await Database.ListCollectionNamesAsync();
        await collections.ForEachAsync(async collectionName =>
        {
            await Database.DropCollectionAsync(collectionName);
        });
    }

    /// <summary>
    /// Creates a fresh HttpClient for each test to avoid shared state
    /// </summary>
    public HttpClient CreateFreshClient()
    {
        return CreateClient();
    }
}