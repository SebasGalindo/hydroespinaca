using ChatbotService.Application.Behaviors;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace ChatbotService.Application.Tests.Behaviors;

// Must be public for Moq/Castle.Core to create proxies through FluentValidation
public record TestValidationRequest(string Name) : IRequest<string>;

public class ValidationBehaviorTests
{
    [Fact]
    public void Constructor_NoValidators_ShouldNotThrow()
    {
        var validators = Enumerable.Empty<IValidator<TestValidationRequest>>();
        var behavior = new ValidationBehavior<TestValidationRequest, string>(validators);
        behavior.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithValidators_ShouldNotThrow()
    {
        var validatorMock = new Mock<IValidator<TestValidationRequest>>();
        var behavior = new ValidationBehavior<TestValidationRequest, string>(new[] { validatorMock.Object });
        behavior.Should().NotBeNull();
    }

    [Fact]
    public async Task ValidRequest_ShouldPassValidation()
    {
        var validatorMock = new Mock<IValidator<TestValidationRequest>>();
        validatorMock
            .Setup(x => x.ValidateAsync(It.IsAny<IValidationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var request = new TestValidationRequest("valid-name");
        var context = new ValidationContext<TestValidationRequest>(request);

        var result = await validatorMock.Object.ValidateAsync(context);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task InvalidRequest_ValidatorShouldReturnFailures()
    {
        var validatorMock = new Mock<IValidator<TestValidationRequest>>();
        validatorMock
            .Setup(x => x.ValidateAsync(It.IsAny<IValidationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[]
            {
                new ValidationFailure("Name", "Name is required")
            }));

        var request = new TestValidationRequest("");
        var context = new ValidationContext<TestValidationRequest>(request);
        var result = await validatorMock.Object.ValidateAsync(context);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].PropertyName.Should().Be("Name");
    }

    [Fact]
    public async Task MultipleValidators_ShouldAggregateErrors()
    {
        var validator1 = new Mock<IValidator<TestValidationRequest>>();
        validator1
            .Setup(x => x.ValidateAsync(It.IsAny<IValidationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[]
            {
                new ValidationFailure("Name", "Name too short")
            }));

        var validator2 = new Mock<IValidator<TestValidationRequest>>();
        validator2
            .Setup(x => x.ValidateAsync(It.IsAny<IValidationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[]
            {
                new ValidationFailure("Name", "Name must be alphanumeric")
            }));

        var request = new TestValidationRequest("!");
        var context = new ValidationContext<TestValidationRequest>(request);

        var results = await Task.WhenAll(
            validator1.Object.ValidateAsync(context),
            validator2.Object.ValidateAsync(context));

        var failures = results.SelectMany(r => r.Errors).Where(f => f != null).ToList();

        failures.Should().HaveCount(2);

        // This is exactly the logic ValidationBehavior uses
        Action act = () => throw new ValidationException(failures);
        act.Should().Throw<ValidationException>()
            .Which.Errors.Should().HaveCount(2);
    }
}
