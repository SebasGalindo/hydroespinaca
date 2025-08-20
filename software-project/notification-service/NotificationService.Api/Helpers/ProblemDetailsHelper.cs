using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace NotificationService.Api.Helpers;

public static class ProblemDetailsHelper
{
    public static ProblemDetails Create(HttpContext ctx, string title, string detail, int statusCode, string type, IWebHostEnvironment env)
    {
        var pd = new ProblemDetails
        {
            Title = title,
            Detail = detail,
            Status = statusCode,
            Type = type,
            Instance = ctx.Request.Path
        };
        if (env.IsDevelopment())
            pd.Extensions["traceId"] = ctx.TraceIdentifier;
        return pd;
    }

    public static ProblemDetails ToProblemDetails(this ValidationException ex, HttpContext ctx)
    {
        var errors = ex.Errors?.GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()) ?? new Dictionary<string, string[]>();
        var pd = new ProblemDetails
        {
            Title = "Validation Failed",
            Detail = "One or more validation errors occurred.",
            Status = StatusCodes.Status400BadRequest,
            Type = "about:blank"
        };
        pd.Extensions["errors"] = errors;
        return pd;
    }

    public static Task WriteProblemDetailsAsync(this HttpContext ctx, ProblemDetails problem, int statusCode)
    {
        ctx.Response.ContentType = "application/problem+json";
        ctx.Response.StatusCode = statusCode;
        return ctx.Response.WriteAsJsonAsync(problem);
    }
}
