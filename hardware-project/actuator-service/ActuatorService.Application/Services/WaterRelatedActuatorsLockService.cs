using System;
using System.Collections.Generic;
using System.Linq;

namespace ActuatorService.Application.Services;

/// <summary>
/// Servicio que maneja el bloqueo de rutinas automáticas relacionadas con agua
/// cuando se detecta la combinación crítica: bomba-agua OFF + piedra-difusora OFF
/// </summary>
public interface IWaterRelatedActuatorsLockService
{
    /// <summary>
    /// Verifica si las rutinas automáticas están bloqueadas
    /// </summary>
    bool IsLocked { get; }

    /// <summary>
    /// Obtiene la fecha y hora en que se activó el bloqueo
    /// </summary>
    DateTime? LockedSince { get; }

    /// <summary>
    /// Evalúa los comandos recibidos y actualiza el estado de bloqueo
    /// </summary>
    /// <param name="actuatorCommands">Lista de comandos de actuadores con sus estados</param>
    void EvaluateCommands(IEnumerable<(string actuatorCode, string power)> actuatorCommands);

    /// <summary>
    /// Limpia manualmente el bloqueo (para debugging/testing)
    /// </summary>
    void ClearLock();

    /// <summary>
    /// Obtiene información detallada del estado actual
    /// </summary>
    WaterLockStatus GetStatus();
}

/// <summary>
/// Estado detallado del bloqueo de actuadores relacionados con agua
/// </summary>
public class WaterLockStatus
{
    public bool IsLocked { get; set; }
    public DateTime? LockedSince { get; set; }
    public TimeSpan? LockedDuration { get; set; }
    public string? Reason { get; set; }
    public Dictionary<string, string> LastKnownStates { get; set; } = new();
}

public class WaterRelatedActuatorsLockService : IWaterRelatedActuatorsLockService
{
    private readonly object _lock = new();

    // Códigos de actuadores críticos relacionados con agua
    private const string WaterPumpCode = "bomba-agua";
    private const string AirStoneCode = "piedra-difusora";

    // Estado del bloqueo
    private bool _isLocked;
    private DateTime? _lockedSince;
    private Dictionary<string, string> _lastKnownStates = new();

    public bool IsLocked
    {
        get
        {
            lock (_lock)
            {
                return _isLocked;
            }
        }
    }

    public DateTime? LockedSince
    {
        get
        {
            lock (_lock)
            {
                return _lockedSince;
            }
        }
    }

    public void EvaluateCommands(IEnumerable<(string actuatorCode, string power)> actuatorCommands)
    {
        lock (_lock)
        {
            // Actualizar estados conocidos con los comandos recibidos
            foreach (var (actuatorCode, power) in actuatorCommands)
            {
                if (actuatorCode == WaterPumpCode || actuatorCode == AirStoneCode)
                {
                    _lastKnownStates[actuatorCode] = power;
                }
            }

            // Verificar si tenemos información de ambos actuadores
            var hasPumpState = _lastKnownStates.TryGetValue(WaterPumpCode, out var pumpPower);
            var hasAirStoneState = _lastKnownStates.TryGetValue(AirStoneCode, out var airStonePower);

            if (!hasPumpState && !hasAirStoneState)
            {
                // No hay información suficiente, mantener estado actual
                return;
            }

            // Detectar combinación crítica: ambos OFF
            var bothOff = hasPumpState && hasAirStoneState
                && pumpPower?.Equals("OFF", StringComparison.OrdinalIgnoreCase) == true
                && airStonePower?.Equals("OFF", StringComparison.OrdinalIgnoreCase) == true;

            if (bothOff && !_isLocked)
            {
                // Activar bloqueo
                _isLocked = true;
                _lockedSince = DateTime.UtcNow;
                Console.WriteLine($"[WaterLock] ⚠️ BLOQUEO ACTIVADO - Combinación crítica detectada: {WaterPumpCode}=OFF, {AirStoneCode}=OFF");
            }
            else if (!bothOff && _isLocked)
            {
                // Desactivar bloqueo: al menos uno de los dos está ON
                var duration = _lockedSince.HasValue
                    ? DateTime.UtcNow - _lockedSince.Value
                    : TimeSpan.Zero;

                Console.WriteLine($"[WaterLock] ✓ BLOQUEO DESACTIVADO - Duración: {duration:hh\\:mm\\:ss}");
                Console.WriteLine($"[WaterLock]   Estados actuales: {WaterPumpCode}={pumpPower}, {AirStoneCode}={airStonePower}");

                _isLocked = false;
                _lockedSince = null;
            }
        }
    }

    public void ClearLock()
    {
        lock (_lock)
        {
            if (_isLocked)
            {
                var duration = _lockedSince.HasValue
                    ? DateTime.UtcNow - _lockedSince.Value
                    : TimeSpan.Zero;

                Console.WriteLine($"[WaterLock] 🔓 BLOQUEO LIMPIADO MANUALMENTE - Duración: {duration:hh\\:mm\\:ss}");
            }

            _isLocked = false;
            _lockedSince = null;
        }
    }

    public WaterLockStatus GetStatus()
    {
        lock (_lock)
        {
            var status = new WaterLockStatus
            {
                IsLocked = _isLocked,
                LockedSince = _lockedSince,
                LastKnownStates = new Dictionary<string, string>(_lastKnownStates)
            };

            if (_isLocked && _lockedSince.HasValue)
            {
                status.LockedDuration = DateTime.UtcNow - _lockedSince.Value;
                status.Reason = $"Combinación crítica detectada: {WaterPumpCode}=OFF y {AirStoneCode}=OFF";
            }

            return status;
        }
    }
}
