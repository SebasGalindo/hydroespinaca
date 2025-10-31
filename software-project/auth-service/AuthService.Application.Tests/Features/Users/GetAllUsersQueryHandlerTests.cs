using AuthService.Application.Features.Users.Queries.GetAllUsers;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AuthService.Domain.ValueObjects;
using AutoMapper;
using HydroEspinaca.Shared.DTOs.Authentication;

namespace AuthService.Application.Tests.Features.Users;

public class GetAllUsersQueryHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly GetAllUsersQueryHandler _handler;

    public GetAllUsersQueryHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _mapperMock = new Mock<IMapper>();
        _handler = new GetAllUsersQueryHandler(_userRepositoryMock.Object, _mapperMock.Object);
    }

    [Fact]
    public async Task Handle_WithUsers_ShouldReturnMappedUsers()
    {
        // Arrange
        var users = new List<User>
        {
            new User("user1", new Email("user1@example.com"), new HashedPassword("$2a$11$hash1")),
            new User("user2", new Email("user2@example.com"), new HashedPassword("$2a$11$hash2"))
        };

        var userDtos = new List<UserResponseDto>
        {
            new UserResponseDto { Id = "1", Username = "user1", Email = "user1@example.com" },
            new UserResponseDto { Id = "2", Username = "user2", Email = "user2@example.com" }
        };

        _userRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(users);
        _mapperMock.Setup(x => x.Map<IEnumerable<UserResponseDto>>(users)).Returns(userDtos);

        var query = new GetAllUsersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(userDtos);
        _userRepositoryMock.Verify(x => x.GetAllAsync(), Times.Once);
        _mapperMock.Verify(x => x.Map<IEnumerable<UserResponseDto>>(users), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNoUsers_ShouldReturnEmptyList()
    {
        // Arrange
        var users = new List<User>();
        var userDtos = new List<UserResponseDto>();

        _userRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(users);
        _mapperMock.Setup(x => x.Map<IEnumerable<UserResponseDto>>(users)).Returns(userDtos);

        var query = new GetAllUsersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
        _userRepositoryMock.Verify(x => x.GetAllAsync(), Times.Once);
    }
}
