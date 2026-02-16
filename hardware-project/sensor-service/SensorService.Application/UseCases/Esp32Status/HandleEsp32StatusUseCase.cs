using HydroEspinaca.Shared.Enums;
using Microsoft.Extensions.Logging;
using SensorService.Application.DTOs.Esp32Status;
using SensorService.Application.Interfaces.UseCases.Esp32Status;
using SensorService.Domain.Constants;
using SensorService.Domain.Entities;
using SensorService.Domain.Interfaces;

namespace SensorService.Application.UseCases.Esp32Status;

/// <summary>
/// Caso de uso para gestionar los cambios de estado de los dispositivos ESP32 recibidos vía MQTT.
/// Maneja las transiciones online/offline/running, actualiza el estado del nodo ESP32
/// y gestiona las alertas de conectividad (creación y resolución automática).
/// </summary>
public class HandleEsp32StatusUseCase : IHandleEsp32StatusUseCase
{
    private readonly IEsp32AlertRepository _esp32AlertRepository;
    private readonly IEsp32NodeRepository _esp32NodeRepository;
    private readonly ILogger<HandleEsp32StatusUseCase> _logger;

    public HandleEsp32StatusUseCase(
        IEsp32AlertRepository esp32AlertRepository,
        IEsp32NodeRepository esp32NodeRepository,
        ILogger<HandleEsp32StatusUseCase> logger)
    {
        _esp32AlertRepository = esp32AlertRepository;
        _esp32NodeRepository = esp32NodeRepository;
        _logger = logger;
    }

    /// <summary>
    /// Procesa la transición a estado online de un ESP32.
    /// Actualiza el estado del nodo con datos de telemetría y resuelve alertas de desconexión activas.
    /// </summary>
    /// <param name="esp32Id">Identificador del ESP32.</param>
    /// <param name="timestamp">Marca de tiempo del evento.</param>
    /// <param name="freeHeap">Memoria heap libre en bytes (opcional).</param>
    /// <param name="uptime">Tiempo de actividad en segundos (opcional).</param>
    public async Task HandleOnlineAsync(string esp32Id, DateTime timestamp, long? freeHeap = null, long? uptime = null)
    {
        _logger.LogInformation("📡 ESP32 {Esp32Id} marcado como online - FreeHeap: {FreeHeap}KB, Uptime: {Uptime}s", 
            esp32Id, freeHeap, uptime);

        // Actualizar ESP32 node a estado online con telemetría
        await SetEsp32OnlineAsync(esp32Id, timestamp, freeHeap ?? 0, uptime ?? 0);

        // Resolver alerta offline activa si existe
        var activeAlert = await _esp32AlertRepository
            .GetActiveByEsp32IdAsync(esp32Id);

        if (activeAlert != null)
        {
            // Resolver la alerta existente con mensaje limpio
            activeAlert.ResolvedAt = timestamp;
            activeAlert.Acknowledged = true;
            activeAlert.Message = AlertMessages.Esp32Offline.Reconnected;
            
            await _esp32AlertRepository.UpdateAsync(activeAlert);
            
            _logger.LogInformation("✅ Alerta resuelta al recibir online para ESP32 {Esp32Id}", esp32Id);
        }
        else
        {
            _logger.LogDebug("ℹ️ No hay alertas activas para resolver para ESP32 {Esp32Id}", esp32Id);
        }
    }

    /// <summary>
    /// Procesa el estado running de un ESP32 (métricas periódicas activas).
    /// Se trata como un estado online: actualiza telemetría y resuelve alertas de desconexión.
    /// </summary>
    /// <param name="esp32Id">Identificador del ESP32.</param>
    /// <param name="timestamp">Marca de tiempo del evento.</param>
    /// <param name="freeHeap">Memoria heap libre en bytes (opcional).</param>
    /// <param name="uptime">Tiempo de actividad en segundos (opcional).</param>
    public async Task HandleRunningAsync(string esp32Id, DateTime timestamp, long? freeHeap = null, long? uptime = null)
    {
        _logger.LogInformation("🔄 ESP32 {Esp32Id} en estado running - FreeHeap: {FreeHeap}KB, Uptime: {Uptime}s", 
            esp32Id, freeHeap, uptime);

        // Actualizar ESP32 node a estado online con telemetría (running se trata como online)
        await SetEsp32OnlineAsync(esp32Id, timestamp, freeHeap ?? 0, uptime ?? 0);

        // Resolver alerta offline activa si existe (igual que online)
        var activeAlert = await _esp32AlertRepository
            .GetActiveByEsp32IdAsync(esp32Id);

        if (activeAlert != null)
        {
            activeAlert.ResolvedAt = timestamp;
            activeAlert.Acknowledged = true;
            activeAlert.Message = AlertMessages.Esp32Offline.Reconnected;
            
            await _esp32AlertRepository.UpdateAsync(activeAlert);
            
            _logger.LogInformation("✅ Alerta resuelta al recibir running para ESP32 {Esp32Id}", esp32Id);
        }
    }

    /// <summary>
    /// Procesa la desconexión de un ESP32 (detectada por MQTT LWT - Last Will and Testament).
    /// Actualiza el estado del nodo a offline y crea una alerta de desconexión si no existe una activa.
    /// </summary>
    /// <param name="esp32Id">Identificador del ESP32.</param>
    /// <param name="timestamp">Marca de tiempo de la desconexión.</param>
    public async Task HandleOfflineAsync(string esp32Id, DateTime timestamp)
    {
        _logger.LogWarning("⚠️ ESP32 {Esp32Id} marcado offline por LWT", esp32Id);

        // Actualizar ESP32 node a estado offline
        await SetEsp32OfflineAsync(esp32Id, timestamp);

        // Verificar si ya existe una alerta activa para evitar duplicados
        var existingAlert = await _esp32AlertRepository
            .GetActiveByEsp32IdAsync(esp32Id);

        if (existingAlert != null)
        {
            _logger.LogDebug("ℹ️ Ya existe una alerta activa para ESP32 {Esp32Id}, no se creará otra", esp32Id);
            return;
        }

        // Crear nueva alerta con mensaje limpio
        var newAlert = new Esp32Alert
        {
            Esp32Id = esp32Id,
            Timestamp = timestamp,
            Message = AlertMessages.Esp32Offline.Disconnected,
            Acknowledged = false,
            ResolvedAt = null
        };

        await _esp32AlertRepository.CreateAsync(newAlert);
        
        _logger.LogInformation("🚨 Nueva alerta creada para ESP32 offline: {Esp32Id}", esp32Id);
    }
    /// <summary>
    /// Procesa un payload completo de estado del ESP32 y delega al handler apropiado.
    /// Valida el estado reportado y enruta a HandleOnlineAsync, HandleRunningAsync o HandleOfflineAsync.
    /// </summary>
    /// <param name="payload">DTO con el payload de estado del ESP32.</param>
    public async Task HandleStatusPayloadAsync(Esp32StatusPayloadDto payload)
    {
        if (!payload.IsValidStatus)
        {
            _logger.LogWarning("⚠️ Payload inválido recibido para ESP32 {Esp32Id}: status='{Status}'", 
                payload.Esp32Id, payload.Status);
            return;
        }

        var timestamp = payload.Timestamp ?? DateTime.UtcNow;

        if (payload.IsOnline)
        {
            await HandleOnlineAsync(payload.Esp32Id, timestamp, payload.FreeHeap, payload.Uptime);
        }
        else if (payload.IsRunning)
        {
            // Running status indicates ESP32 is actively sending periodic metrics
            await HandleRunningAsync(payload.Esp32Id, timestamp, payload.FreeHeap, payload.Uptime);
        }
        else if (payload.IsOffline)
        {
            await HandleOfflineAsync(payload.Esp32Id, timestamp);
        }
    }

    /// <summary>
    /// Actualiza el estado de un nodo ESP32 a online con datos de telemetría.
    /// Busca el nodo por identificador (ObjectId o nombre) y actualiza su estado.
    /// </summary>
    /// <param name="esp32Id">Identificador del ESP32.</param>
    /// <param name="timestamp">Marca de tiempo de la conexión.</param>
    /// <param name="freeHeap">Memoria heap libre en bytes.</param>
    /// <param name="uptime">Tiempo de actividad en segundos.</param>
    private async Task SetEsp32OnlineAsync(string esp32Id, DateTime timestamp, long freeHeap, long uptime)
    {
        try
        {
            // Buscar ESP32 existente usando el identificador (ObjectId o nombre)
            var esp32Node = await _esp32NodeRepository.GetByIdentifierAsync(esp32Id);
            
            if (esp32Node == null)
            {
                _logger.LogWarning("⚠️  ESP32 {Esp32Id} no encontrado en la base de datos - no se puede marcar como online", esp32Id);
                // No crear automáticamente nodos desde mensajes MQTT para evitar spam
                // Los nodos ESP32 deben ser registrados explícitamente desde el API
                return;
            }
            
            // Actualizar estado a online del nodo existente
            esp32Node.SetOnline(timestamp, uptime, freeHeap);
            await _esp32NodeRepository.UpdateAsync(esp32Node);
            
            _logger.LogDebug("✅ ESP32 {Esp32Id} (real ID: {RealId}) marcado como online - FreeHeap: {FreeHeap}KB, Uptime: {Uptime}s", 
                esp32Id, esp32Node.Id, freeHeap, uptime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error configurando ESP32 {Esp32Id} como online", esp32Id);
            // No re-lanzar la excepción para evitar que falle el procesamiento de otros mensajes MQTT
        }
    }

    /// <summary>
    /// Actualiza el estado de un nodo ESP32 a offline.
    /// Busca el nodo por identificador y actualiza su estado de desconexión.
    /// </summary>
    /// <param name="esp32Id">Identificador del ESP32.</param>
    /// <param name="timestamp">Marca de tiempo de la desconexión.</param>
    private async Task SetEsp32OfflineAsync(string esp32Id, DateTime timestamp)
    {
        try
        {
            var esp32Node = await _esp32NodeRepository.GetByIdentifierAsync(esp32Id);
            
            if (esp32Node != null)
            {
                esp32Node.SetOffline(timestamp);
                await _esp32NodeRepository.UpdateAsync(esp32Node);
                _logger.LogDebug("📊 ESP32 {Esp32Id} (real ID: {RealId}) marcado como offline", esp32Id, esp32Node.Id);
            }
            else
            {
                _logger.LogWarning("⚠️ Intentando marcar como offline ESP32 {Esp32Id} que no existe", esp32Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error configurando ESP32 {Esp32Id} como offline", esp32Id);
            // No re-lanzar la excepción para evitar que falle el procesamiento de otros mensajes MQTT
        }
    }
}