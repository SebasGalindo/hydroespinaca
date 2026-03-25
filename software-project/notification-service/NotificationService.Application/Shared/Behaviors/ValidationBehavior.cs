using FluentValidation;
using MediatR;

namespace NotificationService.Application.Shared.Behaviors;

/// <summary>
/// A MediatR pipeline behavior that performs validation on incoming requests using FluentValidation.
/// </summary>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

        /// <summary>
        /// Handles the incoming request by validating it against all registered validators. If any validation failures occur, a ValidationException is 
        /// thrown containing the details of the failures. If validation passes, the next delegate in the pipeline is invoked to continue processing the request.
        /// </summary>
        /// <param name="request">The incoming request to be validated.</param>
        /// <param name="next">The delegate to invoke the next behavior or handler in the pipeline.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>The response from the next delegate in the pipeline if validation succeeds, otherwise throws a ValidationException.</returns>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);
            var validationResults = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

            var failures = validationResults
                .Where(r => r.Errors.Any())
                .SelectMany(r => r.Errors)
                .ToList();

            if (failures.Any())
                throw new ValidationException(failures);
        }

        return await next();
    }
}
