using FluentValidation;

namespace AuthService.Application.Shared.Validators;

/// <summary>
/// Base validator class providing shared validation rules for Auth Service commands.
/// </summary>
public abstract class BaseValidator<T> : AbstractValidator<T>
{
    protected const string EmailRegex = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
    protected const int MinPasswordLength = 6;
    protected const int MaxPasswordLength = 100;
    protected const int ObjectIdLength = 24;
}