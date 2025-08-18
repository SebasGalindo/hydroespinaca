namespace AuthService.Infrastructure.Security;

/// <summary>
/// Implementación en memoria de IKeyStore para usar en tests.
/// Genera claves RSA temporales que son válidas para la duración de la prueba.
/// </summary>
public class InMemoryTestKeyStore : IKeyStore
{
    private readonly string _privateKeyPem;
    private readonly string _publicKeyPem;

    public InMemoryTestKeyStore()
    {
        // Generar un par de claves RSA válidas en memoria para tests
        using var rsa = System.Security.Cryptography.RSA.Create(2048);
        _privateKeyPem = rsa.ExportPkcs8PrivateKeyPem();
        _publicKeyPem = rsa.ExportSubjectPublicKeyInfoPem();
    }

    public string GetPrivateKey()
    {
        return _privateKeyPem;
    }

    public string GetPublicKey()
    {
        return _publicKeyPem;
    }
}