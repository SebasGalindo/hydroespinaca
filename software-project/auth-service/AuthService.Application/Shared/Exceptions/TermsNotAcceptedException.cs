namespace AuthService.Application.Exceptions;

/// <summary>
/// Exception thrown when a user attempts to log in without having accepted the terms and conditions.
/// </summary>
public class TermsNotAcceptedException : Exception
{
    public TermsNotAcceptedException()
        : base("Terms and conditions must be accepted before logging in.") { }
}
