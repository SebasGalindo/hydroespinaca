using Microsoft.AspNetCore.Mvc;
using BffService.Domain.Interfaces;
using BffService.Api.Helpers;

namespace BffService.Api.Controllers.Base;

/// <summary>
/// Base controller for CRUD operations with standardized exception handling.
/// Reduces code duplication by providing reusable CRUD patterns.
/// </summary>
public abstract class CrudControllerBase : BaseAuthenticatedController
{
    protected CrudControllerBase(
        ISessionTokenService sessionTokenService,
        IConfiguration configuration,
        ILogger logger)
        : base(sessionTokenService, configuration, logger)
    {
    }

    /// <summary>
    /// Executes an authenticated operation with centralized exception handling.
    /// </summary>
    protected async Task<IActionResult> ExecuteAuthenticatedAsync<TResult>(
        Func<string, CancellationToken, Task<TResult>> operation,
        string operationContext,
        CancellationToken cancellationToken,
        Func<TResult, IActionResult>? resultSelector = null)
    {
        try
        {
            var session = await ValidateSessionAsync(cancellationToken);
            var result = await operation(session.AccessToken, cancellationToken);

            return resultSelector != null ? resultSelector(result) : Ok(result);
        }
        catch (Exception ex)
        {
            return ControllerExceptionHandler.HandleException(ex, Logger, operationContext);
        }
    }

    /// <summary>
    /// Executes an authenticated operation with identifier parameter and centralized exception handling.
    /// </summary>
    protected async Task<IActionResult> ExecuteAuthenticatedWithIdAsync<TResult>(
        string identifier,
        Func<string, string, CancellationToken, Task<TResult>> operation,
        string operationContext,
        CancellationToken cancellationToken,
        Func<TResult, IActionResult>? resultSelector = null)
    {
        try
        {
            var session = await ValidateSessionAsync(cancellationToken);
            var result = await operation(identifier, session.AccessToken, cancellationToken);

            return resultSelector != null ? resultSelector(result) : Ok(result);
        }
        catch (Exception ex)
        {
            return ControllerExceptionHandler.HandleException(ex, Logger, operationContext, identifier);
        }
    }

    /// <summary>
    /// Executes an authenticated operation with request body and centralized exception handling.
    /// </summary>
    protected async Task<IActionResult> ExecuteAuthenticatedWithBodyAsync<TRequest, TResult>(
        TRequest request,
        Func<TRequest, string, CancellationToken, Task<TResult>> operation,
        string operationContext,
        CancellationToken cancellationToken,
        Func<TResult, IActionResult>? resultSelector = null)
    {
        try
        {
            var session = await ValidateSessionAsync(cancellationToken);
            var result = await operation(request, session.AccessToken, cancellationToken);

            return resultSelector != null ? resultSelector(result) : Ok(result);
        }
        catch (Exception ex)
        {
            return ControllerExceptionHandler.HandleException(ex, Logger, operationContext);
        }
    }

    /// <summary>
    /// Executes an authenticated operation with identifier and request body, with centralized exception handling.
    /// </summary>
    protected async Task<IActionResult> ExecuteAuthenticatedWithIdAndBodyAsync<TRequest, TResult>(
        string identifier,
        TRequest request,
        Func<string, TRequest, string, CancellationToken, Task<TResult>> operation,
        string operationContext,
        CancellationToken cancellationToken,
        Func<TResult, IActionResult>? resultSelector = null)
    {
        try
        {
            var session = await ValidateSessionAsync(cancellationToken);
            var result = await operation(identifier, request, session.AccessToken, cancellationToken);

            return resultSelector != null ? resultSelector(result) : Ok(result);
        }
        catch (Exception ex)
        {
            return ControllerExceptionHandler.HandleException(ex, Logger, operationContext, identifier);
        }
    }

    /// <summary>
    /// Executes an authenticated delete operation with centralized exception handling.
    /// </summary>
    protected async Task<IActionResult> ExecuteAuthenticatedDeleteAsync(
        string identifier,
        Func<string, string, CancellationToken, Task> operation,
        string operationContext,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await ValidateSessionAsync(cancellationToken);
            await operation(identifier, session.AccessToken, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            return ControllerExceptionHandler.HandleException(ex, Logger, operationContext, identifier);
        }
    }

    /// <summary>
    /// Creates a standard NotFound response for null results.
    /// </summary>
    protected IActionResult HandleNullResult<T>(T? result, string resourceName) where T : class
    {
        return result == null
            ? NotFound(new { message = $"{resourceName} not found" })
            : Ok(result);
    }

    /// <summary>
    /// Creates a CreatedAtAction result with the specified action name and route values.
    /// </summary>
    protected IActionResult CreatedResult<T>(string actionName, object routeValues, T value)
    {
        return CreatedAtAction(actionName, routeValues, value);
    }
}
