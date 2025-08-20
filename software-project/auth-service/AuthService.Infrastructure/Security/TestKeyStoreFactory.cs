namespace AuthService.Infrastructure.Security;

/// <summary>
/// Factory para crear una instancia singleton de InMemoryTestKeyStore
/// Garantiza que tanto la generación como validación de tokens usen las mismas claves
/// </summary>
public static class TestKeyStoreFactory
{
    private static InMemoryTestKeyStore? _instance;
    private static readonly object _lock = new object();

    /// <summary>
    /// Obtiene o crea una instancia singleton del InMemoryTestKeyStore
    /// </summary>
    public static InMemoryTestKeyStore GetOrCreateInstance()
    {
        if (_instance == null)
        {
            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = new InMemoryTestKeyStore();
                }
            }
        }
        return _instance;
    }

    /// <summary>
    /// Resetea la instancia (útil para tests que requieren claves frescas)
    /// </summary>
    public static void Reset()
    {
        lock (_lock)
        {
            _instance = null;
        }
    }
}