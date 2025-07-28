using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace SensorService.Api.Helpers;

public static class ProblemDetailsHelper
{
    public static ProblemDetails Create(HttpContext context, string title, string? detail, int statusCode, string type, IWebHostEnvironment env)
    {
        return new ProblemDetails
        {
            Title = title,
            Detail = (env.IsDevelopment() || env.IsStaging()) ? detail : null,
            Status = statusCode,
            Type = type,
            Instance = context.Request.Path
        };
    }
}

public static class ValidationExceptionExtensions
{
    public static ValidationProblemDetails ToProblemDetails(this ValidationException exception, HttpContext context)
    {
        var errors = new Dictionary<string, string[]>();

        if (exception.Errors != null && exception.Errors.Any())
        {
            errors = exception.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => string.IsNullOrEmpty(g.Key) ? "General" : g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );
        }

        return new ValidationProblemDetails(errors)
        {
            Title = "One or more validation errors occurred.",
            Status = StatusCodes.Status400BadRequest,
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
            Instance = context.Request.Path
        };
    }
}

public static class HttpContextExtensions
{
    public static async Task WriteProblemDetailsAsync(this HttpContext context, ProblemDetails problem, int statusCode)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(problem, problem.GetType());
    }
}