using MediatR;
using WeatherService.Domain.Entities;
using WeatherService.Domain.Enums;
using WeatherService.Domain.Interfaces;

namespace WeatherService.Application.Features.Alerts.Commands.SeedAlertConfig;

public class SeedAlertConfigHandler : IRequestHandler<SeedAlertConfigCommand, WeatherAlertConfig>
{
    private readonly IWeatherAlertConfigRepository _repository;

    public SeedAlertConfigHandler(IWeatherAlertConfigRepository repository)
    {
        _repository = repository;
    }

    public async Task<WeatherAlertConfig> Handle(SeedAlertConfigCommand request, CancellationToken cancellationToken)
    {
        if (await _repository.ExistsAsync(request.FuzzySystemId, cancellationToken))
        {
            throw new InvalidOperationException(
                $"Alert config already exists for fuzzy system '{request.FuzzySystemId}'");
        }

        var config = new WeatherAlertConfig
        {
            FuzzySystemId = request.FuzzySystemId,
            FuzzySystemName = request.FuzzySystemName,
            IsActive = true,
            CreatedBy = request.UserId,
            UpdatedBy = request.UserId,
            Alerts = GetDefaultThresholds()
        };

        return await _repository.CreateAsync(config, cancellationToken);
    }

    private static List<AlertThreshold> GetDefaultThresholds() =>
    [
        new()
        {
            Type = AlertTypes.ExtremeHeat,
            Enabled = true,
            ThresholdValue = 35,
            Comparison = "gt",
            Recommendation = "Temperatura exterior alta. Considere aumentar la flexibilidad del ventilador y reducir umbrales de activación del termocalefactor en su rutina fuzzy."
        },
        new()
        {
            Type = AlertTypes.ExtremeCold,
            Enabled = true,
            ThresholdValue = 5,
            Comparison = "lt",
            Recommendation = "Riesgo de helada. Considere reducir umbrales de activación del termocalefactor y calefactor de agua para proteger los cultivos."
        },
        new()
        {
            Type = AlertTypes.HighHumidity,
            Enabled = true,
            ThresholdValue = 90,
            Comparison = "gt",
            Recommendation = "Humedad exterior muy alta. Considere reducir la frecuencia de la bomba de agua y el humidificador en su rutina fuzzy."
        },
        new()
        {
            Type = AlertTypes.LowHumidity,
            Enabled = true,
            ThresholdValue = 30,
            Comparison = "lt",
            Recommendation = "Humedad exterior muy baja. Considere ser más flexible con la activación del humidificador."
        },
        new()
        {
            Type = AlertTypes.HeavyRain,
            Enabled = true,
            ThresholdValue = 10,
            Comparison = "gt",
            Recommendation = "Lluvia intensa pronosticada. Considere reducir la activación de la bomba de agua."
        },
        new()
        {
            Type = AlertTypes.Thunderstorm,
            Enabled = true,
            ThresholdValue = null,
            Comparison = null,
            Recommendation = "Tormenta eléctrica pronosticada. Revise conexiones eléctricas y considere modos de operación conservadores."
        },
        new()
        {
            Type = AlertTypes.HighCloudiness,
            Enabled = true,
            ThresholdValue = 80,
            Comparison = "gt",
            Recommendation = "Nubosidad alta prolongada. Considere ser más flexible con las reglas de activación de la luz de amplio espectro."
        },
        new()
        {
            Type = AlertTypes.StrongWind,
            Enabled = true,
            ThresholdValue = 10,
            Comparison = "gt",
            Recommendation = "Viento fuerte pronosticado. Verifique estructuras del invernadero y ventilación."
        },
        new()
        {
            Type = AlertTypes.ExtremeUv,
            Enabled = true,
            ThresholdValue = 8,
            Comparison = "gt",
            Recommendation = "Índice UV extremo pronosticado. Considere activar sombras o mallas si las tiene disponibles, y evite exposición directa."
        }
    ];
}
