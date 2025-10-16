using AuthService.Application.Features.Users.DTOs;
using AuthService.Domain.Interfaces;
using AuthService.Domain.ValueObjects;
using AutoMapper;
using MediatR;

namespace AuthService.Application.Features.Users.Commands.UpdateUser;

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, UserResponseDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IMapper _mapper;

    public UpdateUserCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IMapper mapper)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _mapper = mapper;
    }

    public async Task<UserResponseDto> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.FindByIdAsync(request.Id);
        if (user == null)
        {
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
                throw new ArgumentException($"Ya existe un usuario con el email '{request.Email}'");
            }
            user.UpdateEmail(new Email(request.Email));
        }

        if (!string.IsNullOrEmpty(request.Password))
        {
            var hashedPassword = _passwordHasher.Hash(request.Password);
            user.UpdatePassword(new HashedPassword(hashedPassword));
        }

        if (request.RoleId != null)
        {
            user.UpdateRoleId(request.RoleId);
        }

        await _userRepository.UpdateAsync(user);

        return _mapper.Map<UserResponseDto>(user);
    }
}