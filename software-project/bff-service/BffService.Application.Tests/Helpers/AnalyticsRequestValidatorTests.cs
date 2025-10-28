using BffService.Application.Helpers;

namespace BffService.Application.Tests.Helpers;

public class AnalyticsRequestValidatorTests
{
    [Theory]
    [InlineData("hourly")]
    [InlineData("daily")]
    [InlineData("weekly")]
    [InlineData("monthly")]
    public void ValidateRequest_WithValidView_DoesNotThrow(string view)
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-1);
        var endDate = DateTime.UtcNow;

        // Act & Assert
        var act = () => AnalyticsRequestValidator.ValidateRequest(view, startDate, endDate);
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("HOURLY")]
    [InlineData("DAILY")]
    [InlineData("Weekly")]
    [InlineData("MONTHLY")]
    public void ValidateRequest_WithValidViewUppercase_DoesNotThrow(string view)
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-1);
        var endDate = DateTime.UtcNow;

        // Act & Assert
        var act = () => AnalyticsRequestValidator.ValidateRequest(view, startDate, endDate);
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("yearly")]
    [InlineData("minutely")]
    [InlineData("")]
    public void ValidateRequest_WithInvalidView_ThrowsInvalidOperationException(string view)
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-1);
        var endDate = DateTime.UtcNow;

        // Act & Assert
        var act = () => AnalyticsRequestValidator.ValidateRequest(view, startDate, endDate);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"Vista inválida '{view}'*");
    }

    [Fact]
    public void ValidateRequest_WithStartDateAfterEndDate_ThrowsInvalidOperationException()
    {
        // Arrange
        var startDate = DateTime.UtcNow;
        var endDate = DateTime.UtcNow.AddDays(-1);

        // Act & Assert
        var act = () => AnalyticsRequestValidator.ValidateRequest("daily", startDate, endDate);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("La fecha inicial no puede ser mayor a la fecha final.");
    }

    [Fact]
    public void ValidateRequest_HourlyView_WithMoreThanOneDay_ThrowsInvalidOperationException()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-2);
        var endDate = DateTime.UtcNow;

        // Act & Assert
        var act = () => AnalyticsRequestValidator.ValidateRequest("hourly", startDate, endDate);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Rango de fechas inválido para vista horaria (máximo 1 día)*");
    }

    [Fact]
    public void ValidateRequest_HourlyView_WithExactlyOneDay_DoesNotThrow()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1, 0, 0, 0);
        var endDate = new DateTime(2024, 1, 2, 0, 0, 0);

        // Act & Assert
        var act = () => AnalyticsRequestValidator.ValidateRequest("hourly", startDate, endDate);
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateRequest_DailyView_WithMoreThan14Days_ThrowsInvalidOperationException()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-15);
        var endDate = DateTime.UtcNow;

        // Act & Assert
        var act = () => AnalyticsRequestValidator.ValidateRequest("daily", startDate, endDate);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Rango de fechas inválido para vista diaria (máximo 14 días)*");
    }

    [Fact]
    public void ValidateRequest_DailyView_WithExactly14Days_DoesNotThrow()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 15);

        // Act & Assert
        var act = () => AnalyticsRequestValidator.ValidateRequest("daily", startDate, endDate);
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateRequest_WeeklyView_WithMoreThan84Days_ThrowsInvalidOperationException()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-85);
        var endDate = DateTime.UtcNow;

        // Act & Assert
        var act = () => AnalyticsRequestValidator.ValidateRequest("weekly", startDate, endDate);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Rango de fechas inválido para vista semanal (máximo 84 días)*");
    }

    [Fact]
    public void ValidateRequest_WeeklyView_WithExactly84Days_DoesNotThrow()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 3, 25); // 84 days later

        // Act & Assert
        var act = () => AnalyticsRequestValidator.ValidateRequest("weekly", startDate, endDate);
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateRequest_MonthlyView_WithMoreThan365Days_ThrowsInvalidOperationException()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-366);
        var endDate = DateTime.UtcNow;

        // Act & Assert
        var act = () => AnalyticsRequestValidator.ValidateRequest("monthly", startDate, endDate);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Rango de fechas inválido para vista mensual (máximo 365 días)*");
    }

    [Fact]
    public void ValidateRequest_MonthlyView_WithExactly365Days_DoesNotThrow()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 12, 31); // Exactly 365 days (non-leap year calculation)

        // Act & Assert
        var act = () => AnalyticsRequestValidator.ValidateRequest("monthly", startDate, endDate);
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateRequest_WithSameDateStartAndEnd_DoesNotThrow()
    {
        // Arrange
        var date = DateTime.UtcNow;

        // Act & Assert
        var act = () => AnalyticsRequestValidator.ValidateRequest("daily", date, date);
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("hourly", 0)]
    [InlineData("daily", 7)]
    [InlineData("weekly", 30)]
    [InlineData("monthly", 180)]
    public void ValidateRequest_WithValidRangeForEachView_DoesNotThrow(string view, int daysBack)
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-daysBack);
        var endDate = DateTime.UtcNow;

        // Act & Assert
        var act = () => AnalyticsRequestValidator.ValidateRequest(view, startDate, endDate);
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateRequest_WithTimeComponentIgnored_ValidatesOnlyDates()
    {
        // Arrange - same day with different times
        var startDate = new DateTime(2024, 1, 1, 0, 0, 0);
        var endDate = new DateTime(2024, 1, 1, 23, 59, 59);

        // Act & Assert - should not throw because it's the same date
        var act = () => AnalyticsRequestValidator.ValidateRequest("hourly", startDate, endDate);
        act.Should().NotThrow();
    }
}
