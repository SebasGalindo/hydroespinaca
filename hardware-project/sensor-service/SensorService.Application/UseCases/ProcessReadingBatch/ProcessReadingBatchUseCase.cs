using FluentValidation;
using SensorService.Application.DTOs.Mqtt;
using SensorService.Application.Interfaces.UseCases.ProcessReadingBatch;
using SensorService.Domain.Interfaces;

namespace SensorService.Application.UseCases.ProcessReadingBatch;
public class ProcessReadingBatchUseCase : IProcessReadingBatchUseCase
{
    private readonly IValidator<ReadingBatchDto> _validator;
    private readonly IReadingRepository _readingRepository;
    private readonly ISensorAlertRepository _alertRepository;
    private readonly IResolveOfflineAlertsUseCase _resolveOfflineAlertsUseCase;
    private readonly IMatchReadingsWithSensorsUseCase _matchReadingsUseCase;
    private readonly IGenerateAlertsUseCase _generateAlertsUseCase;
    private readonly IGenerateInactiveSensorAlertsUseCase _generateInactiveAlertsUseCase;

    public ProcessReadingBatchUseCase(
        IValidator<ReadingBatchDto> validator,
        IReadingRepository readingRepository,
        ISensorAlertRepository alertRepository,
        IResolveOfflineAlertsUseCase resolveOfflineAlertsUseCase,
        IMatchReadingsWithSensorsUseCase matchReadingsUseCase,
        IGenerateAlertsUseCase generateAlertsUseCase,
        IGenerateInactiveSensorAlertsUseCase generateInactiveAlertsUseCase)
    {
        _validator = validator;
        _readingRepository = readingRepository;
        _alertRepository = alertRepository;
        _resolveOfflineAlertsUseCase = resolveOfflineAlertsUseCase;
        _matchReadingsUseCase = matchReadingsUseCase;
        _generateAlertsUseCase = generateAlertsUseCase;
        _generateInactiveAlertsUseCase = generateInactiveAlertsUseCase;
    }

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
            await _resolveOfflineAlertsUseCase.ExecuteAsync(dto.Esp32Id);

            var matchedReadings = await _matchReadingsUseCase.ExecuteAsync(dto);
            var readingsList = matchedReadings.ToList();

            var alerts = await _generateAlertsUseCase.ExecuteAsync(readingsList, dto.Timestamp);
            var alertsList = alerts.ToList();

            var inactiveAlerts = await _generateInactiveAlertsUseCase.ExecuteAsync(dto);
            alertsList.AddRange(inactiveAlerts);

            foreach (var reading in readingsList)
                await _readingRepository.CreateAsync(reading);

            foreach (var alert in alertsList)
                await _alertRepository.CreateAsync(alert);

            var output = new ProcessReadingBatchOutput(readingsList.Count, alertsList.Count);
            return Result<ProcessReadingBatchOutput>.Success(output);
        }
        catch (Exception ex)
        {
            return Result<ProcessReadingBatchOutput>.Failure($"Error processing batch: {ex.Message}");
        }
    }


}
