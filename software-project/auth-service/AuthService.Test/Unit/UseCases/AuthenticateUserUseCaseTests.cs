using AuthService.Application.DTOs;
using AuthService.Application.Interfaces;
using AuthService.Application.UseCases;
using AuthService.Application.Validators;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AuthService.Domain.ValueObjects;
using HydroEspinaca.Shared.Enums;
using HydroEspinaca.Shared.Errors;
using Moq;

namespace AuthService.Tests.Unit.UseCases
{
    public class AuthenticateUserUseCaseTests
    {
        [Fact]
        public async Task ExecuteAsync_ValidCredentials_ReturnsTokens()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var email = "[test@example.com](mailto:test@example.com)";
            var role = "User";
            var fakeUser = new User(new Email(email), new HashedPassword("hash"), Role.User);

            var authServiceMock = new Mock<IAuthenticationService>();
            authServiceMock
                .Setup(s => s.AuthenticateAsync(email, "password"))
                .ReturnsAsync(fakeUser);

            var expectedTokens = new TokenResponseDto
            {
                AccessToken = "access",
                RefreshToken = "refresh",
                ExpiresAt = DateTime.UtcNow.AddMinutes(30),
                Role = role,
                ClientId = null
            };
            var tokenServiceMock = new Mock<ITokenService>();
            tokenServiceMock
                .Setup(s => s.GenerateTokens(userId, email, role, null))
                .Returns(expectedTokens);

            var sut = new AuthenticateUserUseCase(authServiceMock.Object, tokenServiceMock.Object);

            // Act
            var result = await sut.ExecuteAsync(new LoginRequestDto { Email = email, Password = "password" });

            // Assert
            Assert.Equal(expectedTokens, result);
            authServiceMock.VerifyAll();
            tokenServiceMock.VerifyAll();
        }

        [Fact]
        public async Task ExecuteAsync_InvalidCredentials_ThrowsException()
        {
            // Arrange
            var authServiceMock = new Mock<IAuthenticationService>();
            authServiceMock
                .Setup(s => s.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Application.Exceptions.InvalidCredentialsException());

            var tokenServiceMock = new Mock<ITokenService>();
            var sut = new AuthenticateUserUseCase(authServiceMock.Object, tokenServiceMock.Object);

            // Act & Assert
            await Assert.ThrowsAsync<Application.Exceptions.InvalidCredentialsException>(
                () => sut.ExecuteAsync(new LoginRequestDto { Email = "x@x.com", Password = "wrong" })
            );
        }
    }

    public class ClientCredentialsUseCaseTests
    {
        [Fact]
        public async Task ExecuteAsync_ValidClient_ReturnsAccessTokenOnly()
        {
            // Arrange
            var clientId = "client123";
            var secret = "secret";
            var scopes = new[] { "scope1", "scope2" };
            var fakeApp = new ClientApp(clientId, new HashedPassword("hash"), scopes);

            var clientAuthMock = new Mock<IClientAuthenticationService>();
            clientAuthMock
                .Setup(s => s.AuthenticateClientAsync(clientId, secret))
                .ReturnsAsync(fakeApp);

            var tokenDto = new TokenResponseDto { AccessToken = "token", RefreshToken = "ignored", ExpiresAt = DateTime.UtcNow, Role = string.Join(',', scopes), ClientId = clientId };
            var tokenServiceMock = new Mock<ITokenService>();
            tokenServiceMock
                .Setup(s => s.GenerateTokens(Guid.Empty, string.Empty, tokenDto.Role, clientId))
                .Returns(tokenDto);

            var sut = new ClientCredentialsUseCase(clientAuthMock.Object, tokenServiceMock.Object);

            // Act
            var result = await sut.ExecuteAsync(clientId, secret);

            // Assert
            Assert.Equal(tokenDto.AccessToken, result.AccessToken);
            Assert.Equal(string.Empty, result.RefreshToken);
        }

        [Fact]
        public async Task ExecuteAsync_InvalidClient_ThrowsUnauthorized()
        {
            var clientAuthMock = new Mock<IClientAuthenticationService>();
            clientAuthMock
                .Setup(s => s.AuthenticateClientAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new UnauthorizedException("fail"));

            var tokenServiceMock = new Mock<ITokenService>();
            var sut = new ClientCredentialsUseCase(clientAuthMock.Object, tokenServiceMock.Object);

            await Assert.ThrowsAsync<UnauthorizedException>(() => sut.ExecuteAsync("c", "s"));
        }
    }

    public class RefreshTokenUseCaseTests
    {
        [Fact]
        public async Task ExecuteAsync_ValidRefresh_ReturnsNewTokens()
        {
            // Arrange
            var refreshInfo = new RefreshTokenResult(Guid.NewGuid(), "e@e.com", "User", "clientId");
            var refreshTokenMock = new Mock<IRefreshTokenService>();
            refreshTokenMock
                .Setup(s => s.ValidateAndRotateAsync("rtoken"))
                .ReturnsAsync(refreshInfo);

            var expectedTokens = new TokenResponseDto { AccessToken = "a", RefreshToken = "b", ExpiresAt = DateTime.UtcNow, Role = "User", ClientId = "clientId" };
            var tokenServiceMock = new Mock<ITokenService>();
            tokenServiceMock
                .Setup(s => s.GenerateTokens(refreshInfo.UserId, refreshInfo.Email, refreshInfo.Role, refreshInfo.ClientId))
                .Returns(expectedTokens);

            var sut = new RefreshTokenUseCase(refreshTokenMock.Object, tokenServiceMock.Object);

            // Act
            var result = await sut.ExecuteAsync(new RefreshRequestDto { RefreshToken = "rtoken", ClientId = "clientId" });

            // Assert
            Assert.Equal(expectedTokens, result);
        }
    }

    public class ValidateTokenUseCaseTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ExecuteAsync_ReturnsServiceResult(bool valid)
        {
            // Arrange
            var tokenServiceMock = new Mock<ITokenService>();
            tokenServiceMock
                .Setup(s => s.IsTokenValid("tkn"))
                .Returns(valid);

            var sut = new ValidateTokenUseCase(tokenServiceMock.Object);

            // Act
            var result = await sut.ExecuteAsync("tkn");

            // Assert
            Assert.Equal(valid, result);
        }
    }

    public class  Validators
    {
        [Fact]
        public void Validator_Should_Fail_On_Empty_Email_And_Password()
        {
            var validator = new LoginRequestValidator();
            var dto = new LoginRequestDto { Email = "", Password = "" };

            var result = validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "Email");
            Assert.Contains(result.Errors, e => e.PropertyName == "Password");
        }

    }
}
