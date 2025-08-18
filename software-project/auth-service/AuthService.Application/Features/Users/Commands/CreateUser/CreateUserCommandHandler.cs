using AuthService.Application.Features.Users.DTOs;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AuthService.Domain.ValueObjects;
using AutoMapper;
using MediatR;

namespace AuthService.Application.Features.Users.Commands.CreateUser;

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, UserResponseDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IMapper _mapper;

    public CreateUserCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IMapper mapper)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _mapper = mapper;
    }

    public async Task<UserResponseDto> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var existingUser = await _userRepository.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            throw new ArgumentException($"Ya existe un usuario con el email '{request.Email}'");
        }

        var hashedPassword = _passwordHasher.Hash(request.Password);
        var user = new User(
            new Email(request.Email), 
            new HashedPassword(hashedPassword), 
            request.RoleId);

        await _userRepository.CreateAsync(user);

        return _mapper.Map<UserResponseDto>(user);
    }
}