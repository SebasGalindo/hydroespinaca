using HydroEspinaca.Shared.Enums;
using Microsoft.Extensions.Logging;
using SensorService.Application.Interfaces.UseCases.Esp32Status;
using SensorService.Domain.Entities;
using SensorService.Domain.Interfaces;

namespace SensorService.Application.UseCases.Esp32Status;

public class HandleEsp32StatusUseCase : IHandleEsp32StatusUseCase
{
    private readonly IEsp32AlertRepository _esp32AlertRepository;
    private readonly ILogger<HandleEsp32StatusUseCase> _logger;

    public HandleEsp32StatusUseCase(
        IEsp32AlertRepository esp32AlertRepository,
        ILogger<HandleEsp32StatusUseCase> logger)
    {
        _esp32AlertRepository = esp32AlertRepository;
        _logger = logger;
    }

    public async Task HandleOnlineAsync(string esp32Id, DateTime timestamp)
    {
        _logger.LogInformation("📡 ESP32 {Esp32Id} marcado como online por LWT", esp32Id);

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
    }
}