using AuthService.Application.Exceptions;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;

namespace AuthService.Infrastructure.Services;

/// <summary>
/// Infrastructure service implementing user authentication with JWT token generation using RSA-signed tokens.
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly IUserRepository _userRepo;
    private readonly IPasswordHasher _hasher;

    public AuthenticationService(IUserRepository userRepo, IPasswordHasher hasher)
    {
        _userRepo = userRepo;
        _hasher = hasher;
    }

    public async Task<User> AuthenticateAsync(string email, string plainPassword)
    {
        var user = await _userRepo.FindByEmailAsync(email)
                   ?? throw new InvalidCredentialsException();

        if (!_hasher.Verify(user.Password.Value, plainPassword))
            throw new InvalidCredentialsException();

        return user;
    }
}
