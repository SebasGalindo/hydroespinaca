namespace AuthService.Infrastructure.Security
{
    public class FileKeyStore : IKeyStore
    {
        private readonly string _privateKeyPath;
        private readonly string _publicKeyPath;

        public FileKeyStore(string privateKeyPath, string publicKeyPath)
        {
            _privateKeyPath = privateKeyPath;
            _publicKeyPath = publicKeyPath;
        }

        public string GetPrivateKey()
            => File.ReadAllText(_privateKeyPath);

        public string GetPublicKey()
            => File.ReadAllText(_publicKeyPath);
    }
}
