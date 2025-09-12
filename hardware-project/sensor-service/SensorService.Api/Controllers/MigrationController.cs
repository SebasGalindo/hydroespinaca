using Microsoft.AspNetCore.Mvc;
using SensorService.Domain.Interfaces;

namespace SensorService.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MigrationController : ControllerBase
{
    private readonly IVariableMigrationService _migrationService;
    private readonly ILogger<MigrationController> _logger;

    public MigrationController(
        IVariableMigrationService migrationService,
        ILogger<MigrationController> logger)
    {
        _migrationService = migrationService;
        _logger = logger;
    }

    /// <summary>
    /// Migra las variables existentes del esquema legacy (MinValue/MaxValue) al nuevo esquema (Physical/Optimal ranges)
    /// </summary>
    [HttpPost("variables/migrate-to-new-schema")]
    public async Task<IActionResult> MigrateVariablesToNewSchema()
    {
        try
        {
            _logger.LogInformation("🔄 Iniciando migración de variables solicitada via API");
            
            var migratedCount = await _migrationService.MigrateVariablesToNewSchemaAsync();
            
            return Ok(new { 
                Success = true, 
                Message = $"Migración completada exitosamente. {migratedCount} variables actualizadas.",
                MigratedCount = migratedCount 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error durante la migración de variables");
            return StatusCode(500, new { 
                Success = false, 
                Message = "Error interno durante la migración", 
                Error = ex.Message 
            });
        }
    }
}