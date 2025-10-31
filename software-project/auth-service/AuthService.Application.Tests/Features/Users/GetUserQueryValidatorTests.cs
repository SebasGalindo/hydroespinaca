using AuthService.Application.Features.Users.Queries.GetUser;

namespace AuthService.Application.Tests.Features.Users;

public class GetUserQueryValidatorTests
{
    private readonly GetUserQueryValidator _validator;

    public GetUserQueryValidatorTests()
    {
        _validator = new GetUserQueryValidator();
    }

    [Fact]
    public void Validate_ValidId_ShouldPass()
    {
        // Arrange
        var query = new GetUserQuery("507f1f77bcf86cd799439011");

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_EmptyId_ShouldFail(string id)
    {
        // Arrange
        var query = new GetUserQuery(id);

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }

    [Theory]
    [InlineData("123")] // Too short
    [InlineData("507f1f77bcf86cd79943901112345")] // Too long
    public void Validate_InvalidIdLength_ShouldFail(string id)
    {
        // Arrange
        var query = new GetUserQuery(id);

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }
}
