using HydroEspinaca.Shared.DTOs.Readings;

namespace SensorService.Application.Interfaces;

public interface ILatestReadingsService
{
    Task<EnrichedLatestReadingsDto?> GetEnrichedLatestReadingsAsync();
}
