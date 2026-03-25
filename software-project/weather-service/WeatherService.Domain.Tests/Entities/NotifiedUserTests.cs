using WeatherService.Domain.Entities;

namespace WeatherService.Domain.Tests.Entities;

public class NotifiedUserTests
{
    [Fact]
    public void Constructor_SetsDefaults()
    {
        var user = new NotifiedUser { UserId = "user-1" };

        user.Channels.Should().BeEmpty();
        user.IsRead.Should().BeFalse();
    }

    [Fact]
    public void SentAt_DefaultsCloseToUtcNow()
    {
        var before = DateTime.UtcNow;
        var user = new NotifiedUser { UserId = "user-1" };
        var after = DateTime.UtcNow;

        user.SentAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void AllProperties_CanBeSet()
    {
        var now = DateTime.UtcNow;
        var user = new NotifiedUser
        {
            UserId = "user-42",
            Channels = new List<string> { "push", "email" },
            SentAt = now,
            IsRead = true
        };

        user.UserId.Should().Be("user-42");
        user.Channels.Should().HaveCount(2).And.Contain("push").And.Contain("email");
        user.SentAt.Should().Be(now);
        user.IsRead.Should().BeTrue();
    }
}
