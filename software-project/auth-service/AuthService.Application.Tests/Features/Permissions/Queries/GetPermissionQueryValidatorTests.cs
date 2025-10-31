using AuthService.Application.Features.Permissions.Queries.GetPermission;

namespace AuthService.Application.Tests.Features.Permissions.Queries;

public class GetPermissionQueryValidatorTests
{
    private readonly GetPermissionQueryValidator _validator;

    public GetPermissionQueryValidatorTests()
    {
        _validator = new GetPermissionQueryValidator();
    }

    [Fact]
    public void Validate_ValidQueryWithId_ShouldPass()
    {
        // Arrange
        var query = new GetPermissionQuery("507f1f77bcf86cd799439011");

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
        var query = new GetPermissionQuery("PERM_CODE");

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
        var query = new GetPermissionQuery(idOrCode);

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "IdOrCode");
    }
}
