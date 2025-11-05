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
            var commandsList = actuatorCommands.ToList();

            // Verificar si los comandos actuales incluyen alguno de los actuadores críticos
            var hasCriticalActuatorsInCurrentBatch = commandsList.Any(cmd =>
                cmd.actuatorCode == WaterPumpCode || cmd.actuatorCode == AirStoneCode);

            // Si estamos bloqueados y los comandos actuales NO incluyen actuadores críticos,
            // significa que fuzzy-service ya no está enviando comandos de apagado para ellos
            // (la alerta crítica anterior fue un error)
            if (_isLocked && !hasCriticalActuatorsInCurrentBatch)
            {
                var duration = _lockedSince.HasValue
                    ? DateTime.UtcNow - _lockedSince.Value
                    : TimeSpan.Zero;

                Console.WriteLine($"[WaterLock] ✓ BLOQUEO DESACTIVADO - Comandos subsecuentes no incluyen actuadores críticos");
                Console.WriteLine($"[WaterLock]   Duración del bloqueo: {duration:hh\\:mm\\:ss}");
                Console.WriteLine($"[WaterLock]   Razón: fuzzy-service dejó de enviar comandos para {WaterPumpCode}/{AirStoneCode}");

                _isLocked = false;
                _lockedSince = null;
                _lastKnownStates.Clear(); // Limpiar estados previos
                return;
            }

            // Actualizar estados conocidos con los comandos recibidos
            foreach (var (actuatorCode, power) in commandsList)
            {
                if (actuatorCode == WaterPumpCode || actuatorCode == AirStoneCode)
                {
                    _lastKnownStates[actuatorCode] = power;
                }
            }

            // Verificar si tenemos información de ambos actuadores en el batch actual
            var hasPumpInBatch = commandsList.Any(cmd => cmd.actuatorCode == WaterPumpCode);
            var hasAirStoneInBatch = commandsList.Any(cmd => cmd.actuatorCode == AirStoneCode);

            // Solo evaluar combinación crítica si AMBOS actuadores están en el batch actual
            if (hasPumpInBatch && hasAirStoneInBatch)
            {
                var pumpPower = _lastKnownStates[WaterPumpCode];
                var airStonePower = _lastKnownStates[AirStoneCode];

                // Detectar combinación crítica: ambos OFF en el MISMO batch
                var bothOff = pumpPower?.Equals("OFF", StringComparison.OrdinalIgnoreCase) == true
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
                    // Desactivar bloqueo: al menos uno de los dos está ON en este batch
                    var duration = _lockedSince.HasValue
                        ? DateTime.UtcNow - _lockedSince.Value
                        : TimeSpan.Zero;

                    Console.WriteLine($"[WaterLock] ✓ BLOQUEO DESACTIVADO - Al menos un actuador crítico está ON");
                    Console.WriteLine($"[WaterLock]   Duración: {duration:hh\\:mm\\:ss}");
                    Console.WriteLine($"[WaterLock]   Estados actuales: {WaterPumpCode}={pumpPower}, {AirStoneCode}={airStonePower}");

                    _isLocked = false;
                    _lockedSince = null;
                }
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
