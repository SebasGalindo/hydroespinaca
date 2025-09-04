using HydroEspinaca.Shared.Enums;
using Microsoft.Extensions.Logging;
using SensorService.Application.DTOs.Esp32Status;
using SensorService.Application.Interfaces.UseCases.Esp32Status;
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

        // Actualizar telemetría del ESP32 node
        await UpdateEsp32TelemetryAsync(esp32Id, timestamp, freeHeap, uptime);

        // Buscar alerta offline activa para este ESP32
        var activeAlert = await _esp32AlertRepository
            .GetUnacknowledgedByEsp32AndTypeAsync(esp32Id, AlertType.Esp32Offline);

        if (activeAlert != null)
        {
            // Resolver la alerta existente
            activeAlert.ResolvedAt = timestamp;
            activeAlert.Acknowledged = true;
            activeAlert.Message = $"ESP32 '{esp32Id}' se ha reconectado y está enviando datos nuevamente.";
            
            await _esp32AlertRepository.UpdateAsync(activeAlert);
            
            _logger.LogInformation("✅ Alerta resuelta al recibir online para ESP32 {Esp32Id}", esp32Id);
        }
        else
        {
            _logger.LogDebug("ℹ️ No hay alertas activas para resolver para ESP32 {Esp32Id}", esp32Id);
        }
    }

    public async Task HandleOfflineAsync(string esp32Id, DateTime timestamp)
    {
        _logger.LogWarning("⚠️ ESP32 {Esp32Id} marcado offline por LWT", esp32Id);

        // Verificar si ya existe una alerta activa para evitar duplicados
        var existingAlert = await _esp32AlertRepository
            .GetUnacknowledgedByEsp32AndTypeAsync(esp32Id, AlertType.Esp32Offline);

        if (existingAlert != null)
        {
            _logger.LogDebug("ℹ️ Ya existe una alerta activa para ESP32 {Esp32Id}, no se creará otra", esp32Id);
            return;
        }

        // Crear nueva alerta
        var newAlert = new Esp32Alert
        {
            Esp32Id = esp32Id,
            Type = AlertType.Esp32Offline,
            Timestamp = timestamp,
            Severity = AlertSeverity.Critical,
            Message = $"ESP32 '{esp32Id}' se ha desconectado inesperadamente (detectado por MQTT LWT).",
            Acknowledged = false,
            ResolvedAt = null
        };

        await _esp32AlertRepository.CreateAsync(newAlert);
        
        _logger.LogInformation("🚨 Nueva alerta creada para ESP32 offline: {Esp32Id}", esp32Id);

        // Actualizar estado del ESP32 node a offline
        await UpdateEsp32StatusAsync(esp32Id, timestamp, HydroEspinaca.Shared.Enums.Esp32Status.Active);
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
        else if (payload.IsOffline)
        {
            await HandleOfflineAsync(payload.Esp32Id, timestamp);
        }
    }

    private async Task UpdateEsp32TelemetryAsync(string esp32Id, DateTime timestamp, long? freeHeap, long? uptime)
    {
        try
        {
            // Actualizar LastSeen usando el método disponible
            await _esp32NodeRepository.UpdateLastSeenAsync(esp32Id, timestamp);
            
            // Actualizar status a Active
            await _esp32NodeRepository.UpdateStatusAsync(esp32Id, HydroEspinaca.Shared.Enums.Esp32Status.Active);

            _logger.LogDebug("📊 Telemetría actualizada para ESP32 {Esp32Id}: FreeHeap={FreeHeap}, Uptime={Uptime}", 
                esp32Id, freeHeap, uptime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error actualizando telemetría para ESP32 {Esp32Id}", esp32Id);
        }
    }

    private async Task UpdateEsp32StatusAsync(string esp32Id, DateTime timestamp, HydroEspinaca.Shared.Enums.Esp32Status status)
    {
        try
        {
            await _esp32NodeRepository.UpdateLastSeenAsync(esp32Id, timestamp);
            await _esp32NodeRepository.UpdateStatusAsync(esp32Id, status);

            _logger.LogDebug("📊 Status actualizado para ESP32 {Esp32Id}: {Status}", esp32Id, status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error actualizando status para ESP32 {Esp32Id}", esp32Id);
        }
    }
}