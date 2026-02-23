using FluentValidation;
using MediatR;

namespace ChatbotService.Application.Behaviors;

/// <summary>
/// Procesa todas las peticiones con FluentValidation interceptando el request.
/// Lanza ex de validación para ser atrapada por el filter HTTP o middleware devolviendo ProblemDetails.
/// </summary>
public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators) 
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators = validators;

    /// <summary>
    /// Manejador de canalización (interceptor).
    /// </summary>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);
            var validationResults = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));
            var failures = validationResults.SelectMany(r => r.Errors).Where(f => f != null).ToList();

            if (failures.Count != 0)
                throw new FluentValidation.ValidationException(failures);
        }
        return await next();
    }
}
