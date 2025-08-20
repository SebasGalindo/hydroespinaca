using FluentValidation;

namespace AuthService.Application.Shared.Validators;

public abstract class BaseValidator<T> : AbstractValidator<T>
{
    protected const string EmailRegex = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
    protected const int MinPasswordLength = 6;
    protected const int MaxPasswordLength = 100;
    protected const int ObjectIdLength = 24;
}