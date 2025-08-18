using AuthService.Domain.Interfaces;
using AuthService.Domain.ValueObjects;
using HydroEspinaca.Shared.Abstractions;
using MongoDB.Bson;

namespace AuthService.Domain.Entities;
public class ClientApp : IIdentifiableMutable
{
    public string Id { get; private set; } = ObjectId.GenerateNewId().ToString();
    public string Code { get; private set; }
    public HashedPassword Secret { get; private set; }
    public IEnumerable<string> Scopes { get; private set; }

    public ClientApp(string code, HashedPassword secret, IEnumerable<string> scopes)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Client code cannot be null or empty", nameof(code));
        
        Code = code;
        Secret = secret ?? throw new ArgumentNullException(nameof(secret));
        Scopes = scopes ?? Enumerable.Empty<string>();
    }

    public bool VerifySecret(string plainSecret, IPasswordHasher hasher)
    {
        return hasher.Verify(plainSecret, Secret.Value);
    }

    public void SetId(string id)
    {
        Id = id;
    }
}