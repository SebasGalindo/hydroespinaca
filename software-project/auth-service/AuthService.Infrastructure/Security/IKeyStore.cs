namespace AuthService.Infrastructure.Security
{
    /// <summary>
    /// Provides access to the private key for signing and the public key for validating JWTs.
    /// </summary>
    public interface IKeyStore
    {
        string GetPrivateKey();

        string GetPublicKey();
    }
}
