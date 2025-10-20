using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using BffService.Application.Interfaces;
using BffService.Domain.Exceptions;
using BffService.Domain.Interfaces;
using HydroEspinaca.Shared.DTOs.Authentication;

namespace BffService.Api.Controllers;

[ApiController]
[Route("permissions")]
[AllowAnonymous] // We'll validate session manually
public class PermissionController : BaseAuthenticatedController
{
    private readonly IAuthServiceClient _authServiceClient;
    private readonly IMemoryCache _cache;

    private const string GroupedPermissionsCacheKey = "GroupedPermissions";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);

    public PermissionController(
        IAuthServiceClient authServiceClient,
        ISessionTokenService sessionTokenService,
        IMemoryCache cache,
        IConfiguration configuration,
        ILogger<PermissionController> logger)
        : base(sessionTokenService, configuration, logger)
    {
        _authServiceClient = authServiceClient;
        _cache = cache;
    }

    /// <summary>
    /// Get all permissions grouped by category (cached for 24 hours)
    /// </summary>
    [HttpGet("grouped")]
    [ProducesResponseType(typeof(List<GroupedPermissionResponseDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetGrouped(CancellationToken cancellationToken)
    {
        try
        {
            var session = await ValidateSessionAsync(cancellationToken);

            // Try to get from cache first
            if (_cache.TryGetValue(GroupedPermissionsCacheKey, out List<GroupedPermissionResponseDto>? cachedPermissions)
                && cachedPermissions != null)
            {
                Logger.LogDebug("Returning grouped permissions from cache");
                return Ok(cachedPermissions);
            }

            // If not in cache, fetch from auth service
            Logger.LogDebug("Cache miss - fetching grouped permissions from auth service");
            var permissions = await _authServiceClient.GetGroupedPermissionsAsync(session.AccessToken, cancellationToken);

            // Store in cache
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(CacheDuration)
                .SetPriority(CacheItemPriority.High);

            _cache.Set(GroupedPermissionsCacheKey, permissions, cacheEntryOptions);
            Logger.LogDebug("Stored grouped permissions in cache for {Duration}", CacheDuration);

            return Ok(permissions);
        }
        catch (SessionNotFoundException ex)
        {
            Logger.LogWarning(ex, "Session not found");
            return Unauthorized(new { message = "Session not found" });
        }
        catch (SessionExpiredException ex)
        {
            Logger.LogWarning(ex, "Session expired");
            return Unauthorized(new { message = "Session expired, please login again" });
        }
        catch (InvalidTokenException ex)
        {
            Logger.LogWarning(ex, "Invalid or revoked token");
            return Unauthorized(new { message = "Session is no longer valid, please login again" });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting grouped permissions");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get all permissions
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<PermissionResponseDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var session = await ValidateSessionAsync(cancellationToken);
            var permissions = await _authServiceClient.GetAllPermissionsAsync(session.AccessToken, cancellationToken);
            return Ok(permissions);
        }
        catch (SessionNotFoundException ex)
        {
            Logger.LogWarning(ex, "Session not found");
            return Unauthorized(new { message = "Session not found" });
        }
        catch (SessionExpiredException ex)
        {
            Logger.LogWarning(ex, "Session expired");
            return Unauthorized(new { message = "Session expired, please login again" });
        }
        catch (InvalidTokenException ex)
        {
            Logger.LogWarning(ex, "Invalid or revoked token");
            return Unauthorized(new { message = "Session is no longer valid, please login again" });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting permissions");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get permission by code
    /// </summary>
    [HttpGet("{code}")]
    [ProducesResponseType(typeof(PermissionResponseDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetByCode(string code, CancellationToken cancellationToken)
    {
        try
        {
            var session = await ValidateSessionAsync(cancellationToken);
            var permission = await _authServiceClient.GetPermissionByCodeAsync(code, session.AccessToken, cancellationToken);
            if (permission == null)
                return NotFound(new { message = "Permission not found" });

            return Ok(permission);
        }
        catch (SessionNotFoundException ex)
        {
            Logger.LogWarning(ex, "Session not found");
            return Unauthorized(new { message = "Session not found" });
        }
        catch (SessionExpiredException ex)
        {
            Logger.LogWarning(ex, "Session expired");
            return Unauthorized(new { message = "Session expired, please login again" });
        }
        catch (InvalidTokenException ex)
        {
            Logger.LogWarning(ex, "Invalid or revoked token");
            return Unauthorized(new { message = "Session is no longer valid, please login again" });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting permission {PermissionCode}", code);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Create new permission
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(PermissionResponseDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> Create([FromBody] CreatePermissionRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var session = await ValidateSessionAsync(cancellationToken);
            var permission = await _authServiceClient.CreatePermissionAsync(request, session.AccessToken, cancellationToken);

            // Invalidate cache when creating a new permission
            _cache.Remove(GroupedPermissionsCacheKey);
            Logger.LogDebug("Invalidated grouped permissions cache after creating new permission");

            return CreatedAtAction(nameof(GetByCode), new { code = permission.Code }, permission);
        }
        catch (SessionNotFoundException ex)
        {
            Logger.LogWarning(ex, "Session not found");
            return Unauthorized(new { message = "Session not found" });
        }
        catch (SessionExpiredException ex)
        {
            Logger.LogWarning(ex, "Session expired");
            return Unauthorized(new { message = "Session expired, please login again" });
        }
        catch (InvalidTokenException ex)
        {
            Logger.LogWarning(ex, "Invalid or revoked token");
            return Unauthorized(new { message = "Session is no longer valid, please login again" });
        }
        catch (HttpRequestException ex)
        {
            Logger.LogWarning(ex, "Error from auth service creating permission");
            return BadRequest(new { message = "Failed to create permission" });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error creating permission");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Update existing permission
    /// </summary>
    [HttpPut("{code}")]
    [ProducesResponseType(typeof(PermissionResponseDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> Update(string code, [FromBody] UpdatePermissionRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var session = await ValidateSessionAsync(cancellationToken);
            var permission = await _authServiceClient.UpdatePermissionAsync(code, request, session.AccessToken, cancellationToken);

            // Invalidate cache when updating a permission
            _cache.Remove(GroupedPermissionsCacheKey);
            Logger.LogDebug("Invalidated grouped permissions cache after updating permission");

            return Ok(permission);
        }
        catch (SessionNotFoundException ex)
        {
            Logger.LogWarning(ex, "Session not found");
            return Unauthorized(new { message = "Session not found" });
        }
        catch (SessionExpiredException ex)
        {
            Logger.LogWarning(ex, "Session expired");
            return Unauthorized(new { message = "Session expired, please login again" });
        }
        catch (InvalidTokenException ex)
        {
            Logger.LogWarning(ex, "Invalid or revoked token");
            return Unauthorized(new { message = "Session is no longer valid, please login again" });
        }
        catch (HttpRequestException ex)
        {
            Logger.LogWarning(ex, "Error from auth service updating permission");
            return BadRequest(new { message = "Failed to update permission" });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating permission {PermissionCode}", code);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Delete permission
    /// </summary>
    [HttpDelete("{code}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> Delete(string code, CancellationToken cancellationToken)
    {
        try
        {
            var session = await ValidateSessionAsync(cancellationToken);
            await _authServiceClient.DeletePermissionAsync(code, session.AccessToken, cancellationToken);

            // Invalidate cache when deleting a permission
            _cache.Remove(GroupedPermissionsCacheKey);
            Logger.LogDebug("Invalidated grouped permissions cache after deleting permission");

            return NoContent();
        }
        catch (SessionNotFoundException ex)
        {
            Logger.LogWarning(ex, "Session not found");
            return Unauthorized(new { message = "Session not found" });
        }
        catch (SessionExpiredException ex)
        {
            Logger.LogWarning(ex, "Session expired");
            return Unauthorized(new { message = "Session expired, please login again" });
        }
        catch (InvalidTokenException ex)
        {
            Logger.LogWarning(ex, "Invalid or revoked token");
            return Unauthorized(new { message = "Session is no longer valid, please login again" });
        }
        catch (HttpRequestException ex)
        {
            Logger.LogWarning(ex, "Error from auth service deleting permission");
            return NotFound(new { message = "Permission not found" });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting permission {PermissionCode}", code);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}
