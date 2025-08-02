using AuthService.Domain.Interfaces;
using AuthService.Domain.ValueObjects;
using System.Collections;

namespace AuthService.Domain.Entities;
public class ClientApp
{
    public Guid Id { get; private set; }
    public string ClientId { get; private set; }
    public HashedPassword Secret { get; private set; }
    public IEnumerable Scopes { get; private set; }
    public ClientApp(string clientId, HashedPassword secret, IEnumerable<string> scopes)
    {
        Id = Guid.NewGuid();
        ClientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
        Secret = secret ?? throw new ArgumentNullException(nameof(secret));
        Scopes = scopes ?? Enumerable.Empty<string>();
    }

    public bool VerifySecret(string plainSecret, IPasswordHasher hasher)
    {
        return hasher.Verify(plainSecret, Secret.Value);
    }
}