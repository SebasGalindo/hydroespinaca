using AuthService.Domain.Enums;
using AuthService.Infrastructure.Security.Models;
using System.Text.Json;

namespace AuthService.Infrastructure.Security
{
    /// <summary>
    /// File-system based RSA key store for JWT signing and validation. Loads or generates RSA key pairs from disk.
    /// </summary>
    public class FileKeyStore : IKeyStore
    {
        private readonly string _keysDirectory;
        private readonly Dictionary<TokenType, JwtKeyPair> _keyPairs;
        private readonly Dictionary<string, JwtKeyPair> _keyPairsById;

        public FileKeyStore(string keysDirectory)
        {
            _keysDirectory = keysDirectory;
            _keyPairs = new Dictionary<TokenType, JwtKeyPair>();
            _keyPairsById = new Dictionary<string, JwtKeyPair>();
            
            LoadOrGenerateKeyPairs();
        }

        public JwtKeyPair GetKeyPair(TokenType tokenType)
        {
            if (_keyPairs.TryGetValue(tokenType, out var keyPair))
            {
                return keyPair;
            }
            
            throw new InvalidOperationException($"No key pair found for token type: {tokenType}");
        }

        public IEnumerable<JwtKeyPair> GetAllKeyPairs()
        {
            return _keyPairs.Values;
        }

        public JwtKeyPair? GetKeyPairById(string keyId)
        {
            _keyPairsById.TryGetValue(keyId, out var keyPair);
            return keyPair;
        }

        private void LoadOrGenerateKeyPairs()
        {
            // Ensure keys directory exists
            if (!Directory.Exists(_keysDirectory))
            {
                Directory.CreateDirectory(_keysDirectory);
            }

            // Load or generate key pairs for each token type
            foreach (TokenType tokenType in Enum.GetValues<TokenType>())
            {
                var keyPair = LoadOrGenerateKeyPair(tokenType);
                _keyPairs[tokenType] = keyPair;
                _keyPairsById[keyPair.KeyId] = keyPair;
            }
        }

        private JwtKeyPair LoadOrGenerateKeyPair(TokenType tokenType)
        {
            var configFile = Path.Combine(_keysDirectory, $"{tokenType.ToString().ToLower()}-config.json");
            var privateKeyFile = Path.Combine(_keysDirectory, $"{tokenType.ToString().ToLower()}-private.pem");
            var publicKeyFile = Path.Combine(_keysDirectory, $"{tokenType.ToString().ToLower()}-public.pem");

            // Try to load existing configuration
            if (File.Exists(configFile))
            {
                try
                {
                    var configJson = File.ReadAllText(configFile);
                    var config = JsonSerializer.Deserialize<KeyPairConfig>(configJson);
                    
                    if (config != null && 
                        File.Exists(privateKeyFile) && 
                        File.Exists(publicKeyFile))
                    {
                        return new JwtKeyPair
                        {
                            KeyId = config.KeyId,
                            TokenType = tokenType,
                            PrivateKey = File.ReadAllText(privateKeyFile),
                            PublicKey = File.ReadAllText(publicKeyFile)
                        };
                    }
                }
                catch
                {
                    // If loading fails, generate new keys
                }
            }

            // Generate new key pair
            var keyId = RsaKeyGenerator.GenerateDefaultKeyId(tokenType);
            var keyPair = RsaKeyGenerator.GenerateKeyPair(keyId, tokenType);

            // Save to files
            var keyConfig = new KeyPairConfig { KeyId = keyId, TokenType = tokenType };
            File.WriteAllText(configFile, JsonSerializer.Serialize(keyConfig, new JsonSerializerOptions { WriteIndented = true }));
            File.WriteAllText(privateKeyFile, keyPair.PrivateKey);
            File.WriteAllText(publicKeyFile, keyPair.PublicKey);

            return keyPair;
        }

        private class KeyPairConfig
        {
            public string KeyId { get; set; } = default!;
            public TokenType TokenType { get; set; }
        }
    }
}
