using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using BffService.Application.Interfaces;
using BffService.Domain.Interfaces;
using BffService.Api.Controllers.Base;
using HydroEspinaca.Shared.DTOs.Authentication;

namespace BffService.Api.Controllers;

[ApiController]
[Route("permissions")]
[AllowAnonymous] // We'll validate session manually
public class PermissionController : CrudControllerBase
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
        return await ExecuteAuthenticatedAsync(
            async (accessToken, ct) =>
            {
                // Try to get from cache first
                if (_cache.TryGetValue(GroupedPermissionsCacheKey, out List<GroupedPermissionResponseDto>? cachedPermissions)
                    && cachedPermissions != null)
                {
                    Logger.LogDebug("Returning grouped permissions from cache");
                    return cachedPermissions;
                }

                // If not in cache, fetch from auth service
                Logger.LogDebug("Cache miss - fetching grouped permissions from auth service");
                var permissions = await _authServiceClient.GetGroupedPermissionsAsync(accessToken, ct);

                // Store in cache
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(CacheDuration)
                    .SetPriority(CacheItemPriority.High);

                _cache.Set(GroupedPermissionsCacheKey, permissions, cacheEntryOptions);
                Logger.LogDebug("Stored grouped permissions in cache for {Duration}", CacheDuration);

                return permissions;
            },
            "getting grouped permissions",
            cancellationToken);
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
        return await ExecuteAuthenticatedAsync(
            _authServiceClient.GetAllPermissionsAsync,
            "getting permissions",
            cancellationToken);
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
        return await ExecuteAuthenticatedWithIdAsync(
            code,
            _authServiceClient.GetPermissionByCodeAsync,
            "getting permission",
            cancellationToken,
            result => HandleNullResult(result, "Permission"));
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
        return await ExecuteAuthenticatedWithBodyAsync(
            request,
            async (req, accessToken, ct) =>
            {
                var permission = await _authServiceClient.CreatePermissionAsync(req, accessToken, ct);

                // Invalidate cache when creating a new permission
                _cache.Remove(GroupedPermissionsCacheKey);
                Logger.LogDebug("Invalidated grouped permissions cache after creating new permission");

                return permission;
            },
            "creating permission",
            cancellationToken,
            result => CreatedResult(nameof(GetByCode), new { code = result.Code }, result));
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
        return await ExecuteAuthenticatedWithIdAndBodyAsync(
            code,
            request,
            async (id, req, accessToken, ct) =>
            {
                var permission = await _authServiceClient.UpdatePermissionAsync(id, req, accessToken, ct);

                // Invalidate cache when updating a permission
                _cache.Remove(GroupedPermissionsCacheKey);
                Logger.LogDebug("Invalidated grouped permissions cache after updating permission");

                return permission;
            },
            "updating permission",
            cancellationToken);
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
        return await ExecuteAuthenticatedDeleteAsync(
            code,
            async (id, accessToken, ct) =>
            {
                await _authServiceClient.DeletePermissionAsync(id, accessToken, ct);

                // Invalidate cache when deleting a permission
                _cache.Remove(GroupedPermissionsCacheKey);
                Logger.LogDebug("Invalidated grouped permissions cache after deleting permission");
            },
            "deleting permission",
            cancellationToken);
    }
}
