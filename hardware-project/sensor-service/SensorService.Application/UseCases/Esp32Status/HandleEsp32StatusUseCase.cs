using HydroEspinaca.Shared.Enums;
using Microsoft.Extensions.Logging;
using SensorService.Application.DTOs.Esp32Status;
using SensorService.Application.Interfaces.UseCases.Esp32Status;
using SensorService.Domain.Constants;
using SensorService.Domain.Entities;
using SensorService.Domain.Interfaces;

namespace SensorService.Application.UseCases.Esp32Status;

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

    public async Task HandleOnlineAsync(string esp32Id, DateTime timestamp, long? freeHeap = null, long? uptime = null)
    {
        _logger.LogInformation("📡 ESP32 {Esp32Id} marcado como online - FreeHeap: {FreeHeap}KB, Uptime: {Uptime}s", 
            esp32Id, freeHeap, uptime);

        // Actualizar ESP32 node a estado online con telemetría
        await SetEsp32OnlineAsync(esp32Id, timestamp, freeHeap ?? 0, uptime ?? 0);

        // Resolver alerta offline activa si existe
        var activeAlert = await _esp32AlertRepository
            .GetUnacknowledgedByEsp32AndTypeAsync(esp32Id, AlertType.Esp32Offline);

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

    public async Task HandleRunningAsync(string esp32Id, DateTime timestamp, long? freeHeap = null, long? uptime = null)
    {
        _logger.LogInformation("🔄 ESP32 {Esp32Id} en estado running - FreeHeap: {FreeHeap}KB, Uptime: {Uptime}s", 
            esp32Id, freeHeap, uptime);

        // Actualizar ESP32 node a estado online con telemetría (running se trata como online)
        await SetEsp32OnlineAsync(esp32Id, timestamp, freeHeap ?? 0, uptime ?? 0);

        // Resolver alerta offline activa si existe (igual que online)
        var activeAlert = await _esp32AlertRepository
            .GetUnacknowledgedByEsp32AndTypeAsync(esp32Id, AlertType.Esp32Offline);

        if (activeAlert != null)
        {
            activeAlert.ResolvedAt = timestamp;
            activeAlert.Acknowledged = true;
            activeAlert.Message = AlertMessages.Esp32Offline.Reconnected;
            
            await _esp32AlertRepository.UpdateAsync(activeAlert);
            
            _logger.LogInformation("✅ Alerta resuelta al recibir running para ESP32 {Esp32Id}", esp32Id);
        }
    }

    public async Task HandleOfflineAsync(string esp32Id, DateTime timestamp)
    {
        _logger.LogWarning("⚠️ ESP32 {Esp32Id} marcado offline por LWT", esp32Id);

        // Actualizar ESP32 node a estado offline
        await SetEsp32OfflineAsync(esp32Id, timestamp);

        // Verificar si ya existe una alerta activa para evitar duplicados
        var existingAlert = await _esp32AlertRepository
            .GetUnacknowledgedByEsp32AndTypeAsync(esp32Id, AlertType.Esp32Offline);

        if (existingAlert != null)
        {
            _logger.LogDebug("ℹ️ Ya existe una alerta activa para ESP32 {Esp32Id}, no se creará otra", esp32Id);
            return;
        }

        // Crear nueva alerta con mensaje limpio
        var newAlert = new Esp32Alert
        {
            Esp32Id = esp32Id,
            Type = AlertType.Esp32Offline,
            Timestamp = timestamp,
            Severity = AlertSeverity.Critical,
            Message = AlertMessages.Esp32Offline.Disconnected,
            Acknowledged = false,
            ResolvedAt = null
        };

        await _esp32AlertRepository.CreateAsync(newAlert);
        
        _logger.LogInformation("🚨 Nueva alerta creada para ESP32 offline: {Esp32Id}", esp32Id);
    }

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