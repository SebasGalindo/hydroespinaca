using AuthService.Application.Features.Users.Queries.GetUser;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AuthService.Domain.ValueObjects;
using AutoMapper;
using HydroEspinaca.Shared.DTOs.Authentication;

namespace AuthService.Application.Tests.Features.Users;

public class GetUserQueryHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly GetUserQueryHandler _handler;

    public GetUserQueryHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _mapperMock = new Mock<IMapper>();
        _handler = new GetUserQueryHandler(_userRepositoryMock.Object, _mapperMock.Object);
    }

    [Fact]
    public async Task Handle_UserExists_ShouldReturnMappedUser()
    {
        // Arrange
        var userId = "507f1f77bcf86cd799439011";
        var user = new User("testuser", new Email("test@example.com"), new HashedPassword("$2a$11$hash"));
        user.SetId(userId);

        var userDto = new UserResponseDto
        {
            Id = userId,
            Username = "testuser",
            Email = "test@example.com"
        };

        _userRepositoryMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
        _mapperMock.Setup(x => x.Map<UserResponseDto>(user)).Returns(userDto);

        var query = new GetUserQuery(userId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(userDto);
        _userRepositoryMock.Verify(x => x.FindByIdAsync(userId), Times.Once);
        _mapperMock.Verify(x => x.Map<UserResponseDto>(user), Times.Once);
    }

    [Fact]
    public async Task Handle_UserNotFound_ShouldReturnNull()
    {
        // Arrange
        var userId = "507f1f77bcf86cd799439011";
        _userRepositoryMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync((User)null!);

        var query = new GetUserQuery(userId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
        _userRepositoryMock.Verify(x => x.FindByIdAsync(userId), Times.Once);
        _mapperMock.Verify(x => x.Map<UserResponseDto>(It.IsAny<User>()), Times.Never);
    }
}
