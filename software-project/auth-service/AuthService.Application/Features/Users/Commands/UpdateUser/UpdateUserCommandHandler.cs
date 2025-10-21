using HydroEspinaca.Shared.DTOs.Authentication;
using AuthService.Domain.Interfaces;
using AuthService.Domain.ValueObjects;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AuthService.Application.Features.Users.Commands.UpdateUser;

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, UserResponseDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateUserCommandHandler> _logger;

    public UpdateUserCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IMapper mapper,
        ILogger<UpdateUserCommandHandler> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<UserResponseDto> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating user with ID: {UserId}. Fields to update - Username: {HasUsername}, Email: {HasEmail}, Password: {HasPassword}, RoleId: {HasRoleId}",
            request.Id,
            !string.IsNullOrEmpty(request.Username),
            !string.IsNullOrEmpty(request.Email),
            !string.IsNullOrEmpty(request.Password),
            request.RoleId != null);

        var user = await _userRepository.FindByIdAsync(request.Id);
        if (user == null)
        {
            _logger.LogWarning("User not found with ID: {UserId}", request.Id);
            throw new ArgumentException($"No se encontró el usuario con ID '{request.Id}'");
        }

        if (!string.IsNullOrEmpty(request.Username))
        {
            user.UpdateUsername(request.Username);
        }

        if (!string.IsNullOrEmpty(request.Email) && request.Email != user.Email.Value)
        {
            var existingUser = await _userRepository.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                _logger.LogWarning("Cannot update user {UserId}: Email {Email} already exists", request.Id, request.Email);
                throw new ArgumentException($"Ya existe un usuario con el email '{request.Email}'");
            }
            _logger.LogDebug("Updating email for user {UserId} from {OldEmail} to {NewEmail}", request.Id, user.Email.Value, request.Email);
            user.UpdateEmail(new Email(request.Email));
        }

        if (!string.IsNullOrEmpty(request.Password))
        {
            _logger.LogDebug("Updating password for user {UserId}", request.Id);
            var hashedPassword = _passwordHasher.Hash(request.Password);
            user.UpdatePassword(new HashedPassword(hashedPassword));
        }

        if (request.RoleId != null)
        {
            _logger.LogDebug("Updating roleId for user {UserId} from {OldRoleId} to {NewRoleId}", request.Id, user.RoleId, request.RoleId);
            user.UpdateRoleId(request.RoleId);
        }

        await _userRepository.UpdateAsync(user);
        _logger.LogInformation("Successfully updated user {UserId}", request.Id);

        return _mapper.Map<UserResponseDto>(user);
    }
}