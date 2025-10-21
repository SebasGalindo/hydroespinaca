namespace ActuatorService.Application.Helpers;

/// <summary>
/// Helper class for generating unique command IDs.
/// </summary>
public static class CommandIdHelper
{
    /// <summary>
    /// Generates a unique command ID using the actuator code and current timestamp.
    /// Format: {actuatorCode}_{unixTimestamp}
    /// </summary>
    /// <param name="actuatorCode">The actuator code (e.g., "BombaRiego", "Ventiladores")</param>
    /// <returns>A unique command ID string</returns>
    public static string GenerateCommandId(string actuatorCode)
    {
        return $"{actuatorCode}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
    }
}
