using FluentValidation;
using HydroEspinaca.Shared.DTOs.Mqtt;
using SensorService.Application.Interfaces;
using SensorService.Application.Interfaces.UseCases.Esp32;
using SensorService.Application.Interfaces.UseCases.ProcessReadingBatch;
using SensorService.Domain.Entities;
using SensorService.Domain.Interfaces;

namespace SensorService.Application.UseCases.ProcessReadingBatch;

/// <summary>
/// Caso de uso principal para procesar un lote de lecturas de sensores recibido vía MQTT.
/// Orquesta el flujo completo: validación del lote → emparejamiento con sensores →
/// actualización de última actividad del ESP32 → generación de alertas →
/// persistencia de lecturas y alertas → procesamiento de alertas críticas.
/// </summary>
public class ProcessReadingBatchUseCase : IProcessReadingBatchUseCase
{
    private readonly IValidator<ReadingBatchDto> _validator;
    private readonly IReadingRepository _readingRepository;
    private readonly ISensorAlertRepository _alertRepository;
    private readonly IMatchReadingsWithSensorsUseCase _matchReadingsUseCase;
    private readonly IGenerateAlertsUseCase _generateAlertsUseCase;
    private readonly IGenerateInactiveSensorAlertsUseCase _generateInactiveAlertsUseCase;
    private readonly IUpdateEsp32LastSeenUseCase _updateEsp32LastSeenUseCase;
    private readonly ICriticalAlertApplicationService _criticalAlertService;

    public ProcessReadingBatchUseCase(
        IValidator<ReadingBatchDto> validator,
        IReadingRepository readingRepository,
        ISensorAlertRepository alertRepository,
        IMatchReadingsWithSensorsUseCase matchReadingsUseCase,
        IGenerateAlertsUseCase generateAlertsUseCase,
        IGenerateInactiveSensorAlertsUseCase generateInactiveAlertsUseCase,
        IUpdateEsp32LastSeenUseCase updateEsp32LastSeenUseCase,
        ICriticalAlertApplicationService criticalAlertService
        )
    {
        _validator = validator;
        _readingRepository = readingRepository;
        _alertRepository = alertRepository;
        _matchReadingsUseCase = matchReadingsUseCase;
        _generateAlertsUseCase = generateAlertsUseCase;
        _generateInactiveAlertsUseCase = generateInactiveAlertsUseCase;
        _updateEsp32LastSeenUseCase = updateEsp32LastSeenUseCase;
        _criticalAlertService = criticalAlertService;
    }

    /// <summary>
    /// Ejecuta el procesamiento completo de un lote de lecturas.
    /// Valida el DTO, empareja lecturas con sensores registrados, genera alertas
    /// de rango y de inactividad, persiste los datos y procesa notificaciones críticas.
    /// </summary>
    /// <param name="dto">DTO del lote con ID del ESP32, marca de tiempo y lecturas.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Resultado exitoso con conteos o resultado de error con mensaje descriptivo.</returns>
    public async Task<Result<ProcessReadingBatchOutput>> ExecuteAsync(ReadingBatchDto dto, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}");
            return Result<ProcessReadingBatchOutput>.Failure(string.Join(" | ", errors));
        }

        try
        {
            // Nota: Con MQTT LWT el sistema maneja automáticamente el estado online/offline
            // No es necesario resolver manualmente las alertas offline aquí
            
            var matchedReadings = (await _matchReadingsUseCase.ExecuteAsync(dto)).ToList();

            if (matchedReadings.Any())
                await _updateEsp32LastSeenUseCase.ExecuteAsync(dto.Esp32Id, dto.Timestamp.DateTime);

            var sensorAlerts = (await _generateAlertsUseCase.ExecuteAsync(matchedReadings, dto.Timestamp.DateTime)).ToList();
            var inactiveAlerts = await _generateInactiveAlertsUseCase.ExecuteAsync(dto);

            sensorAlerts.AddRange(inactiveAlerts);

            await PersistReadingsAsync(matchedReadings);
            await PersistAlertsAsync(sensorAlerts);

            // Process critical alert notifications for manual variables (pH, EC, Water Level)
            await _criticalAlertService.ProcessCriticalAlertsAsync(dto.Esp32Id, dto.Timestamp.DateTime, matchedReadings, cancellationToken);

            var output = new ProcessReadingBatchOutput(matchedReadings.Count, sensorAlerts.Count);
            return Result<ProcessReadingBatchOutput>.Success(output);
        }
        catch (Exception ex)
        {
            return Result<ProcessReadingBatchOutput>.Failure($"Error processing batch: {ex.Message}");
        }
    }

    /// <summary>
    /// Persiste las lecturas emparejadas en el repositorio de lecturas.
    /// </summary>
    /// <param name="readings">Lecturas a persistir.</param>
    private async Task PersistReadingsAsync(IEnumerable<Reading> readings)
    {
        foreach (var reading in readings)
            await _readingRepository.CreateAsync(reading);
    }

    /// <summary>
    /// Persiste las alertas generadas en el repositorio de alertas de sensores.
    /// </summary>
    /// <param name="alerts">Alertas a persistir.</param>
    private async Task PersistAlertsAsync(IEnumerable<SensorAlert> alerts)
    {
        foreach (var alert in alerts)
            await _alertRepository.CreateAsync(alert);
    }
}
