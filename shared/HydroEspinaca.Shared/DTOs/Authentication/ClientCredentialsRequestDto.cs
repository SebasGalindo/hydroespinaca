namespace HydroEspinaca.Shared.DTOs.Authentication;

/// <summary>
/// Shared client credentials request DTO used across all authentication services
/// </summary>
public record ClientCredentialsRequestDto(
    string ClientId,
    string ClientSecret
);