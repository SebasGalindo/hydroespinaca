using AuthService.Application.Features.Roles.Queries.GetRole;

namespace AuthService.Application.Tests.Features.Roles.Queries;

public class GetRoleQueryValidatorTests
{
    private readonly GetRoleQueryValidator _validator;

    public GetRoleQueryValidatorTests()
    {
        _validator = new GetRoleQueryValidator();
    }

    [Fact]
    public void Validate_ValidQueryWithId_ShouldPass()
    {
        // Arrange
        var query = new GetRoleQuery("507f1f77bcf86cd799439011");

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_ValidQueryWithCode_ShouldPass()
    {
        // Arrange
        var query = new GetRoleQuery("ADMIN");

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_EmptyIdOrCode_ShouldFail(string idOrCode)
    {
        // Arrange
        var query = new GetRoleQuery(idOrCode);

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "IdOrCode");
    }
}
