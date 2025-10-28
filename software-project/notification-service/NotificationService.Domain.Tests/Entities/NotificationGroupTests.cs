using NotificationService.Domain.Entities;

namespace NotificationService.Domain.Tests.Entities;

public class NotificationGroupTests
{
    [Fact]
    public void SetId_ShouldSetIdProperty()
    {
        // Arrange
        var group = new NotificationGroup { GroupName = "Test Group" };
        var expectedId = "test-id-123";

        // Act
        group.SetId(expectedId);

        // Assert
        group.Id.Should().Be(expectedId);
    }

    [Fact]
    public void UpdateTimestamp_ShouldUpdateUpdatedAtProperty()
    {
        // Arrange
        var group = new NotificationGroup { GroupName = "Test Group" };
        var initialTimestamp = group.UpdatedAt;
        Thread.Sleep(10); // Pequeña espera para asegurar diferencia de tiempo

        // Act
        group.UpdateTimestamp();

        // Assert
        group.UpdatedAt.Should().BeAfter(initialTimestamp);
    }

    [Fact]
    public void HasActiveRecipients_WithActiveRecipients_ReturnsTrue()
    {
        // Arrange
        var group = new NotificationGroup
        {
            GroupName = "Test Group",
            Recipients = new List<GroupRecipient>
            {
                new() { Email = "active@test.com", IsActive = true },
                new() { Email = "inactive@test.com", IsActive = false }
            }
        };

        // Act
        var result = group.HasActiveRecipients();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasActiveRecipients_WithNoActiveRecipients_ReturnsFalse()
    {
        // Arrange
        var group = new NotificationGroup
        {
            GroupName = "Test Group",
            Recipients = new List<GroupRecipient>
            {
                new() { Email = "inactive1@test.com", IsActive = false },
                new() { Email = "inactive2@test.com", IsActive = false }
            }
        };

        // Act
        var result = group.HasActiveRecipients();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HasActiveRecipients_WithEmptyRecipients_ReturnsFalse()
    {
        // Arrange
        var group = new NotificationGroup
        {
            GroupName = "Test Group",
            Recipients = new List<GroupRecipient>()
        };

        // Act
        var result = group.HasActiveRecipients();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetRecipientsByType_ShouldGroupRecipientsByType()
    {
        // Arrange
        var group = new NotificationGroup
        {
            GroupName = "Test Group",
            Recipients = new List<GroupRecipient>
            {
                new() { Email = "to1@test.com", Type = RecipientType.TO, IsActive = true },
                new() { Email = "to2@test.com", Type = RecipientType.TO, IsActive = true },
                new() { Email = "cc1@test.com", Type = RecipientType.CC, IsActive = true },
                new() { Email = "bcc1@test.com", Type = RecipientType.BCC, IsActive = true },
                new() { Email = "inactive@test.com", Type = RecipientType.TO, IsActive = false }
            }
        };

        // Act
        var (to, cc, bcc) = group.GetRecipientsByType();

        // Assert
        to.Should().HaveCount(2);
        to.Should().Contain(new[] { "to1@test.com", "to2@test.com" });

        cc.Should().HaveCount(1);
        cc.Should().Contain("cc1@test.com");

        bcc.Should().HaveCount(1);
        bcc.Should().Contain("bcc1@test.com");
    }

    [Fact]
    public void GetRecipientsByType_ShouldExcludeInactiveRecipients()
    {
        // Arrange
        var group = new NotificationGroup
        {
            GroupName = "Test Group",
            Recipients = new List<GroupRecipient>
            {
                new() { Email = "active@test.com", Type = RecipientType.TO, IsActive = true },
                new() { Email = "inactive1@test.com", Type = RecipientType.TO, IsActive = false },
                new() { Email = "inactive2@test.com", Type = RecipientType.CC, IsActive = false }
            }
        };

        // Act
        var (to, cc, bcc) = group.GetRecipientsByType();

        // Assert
        to.Should().ContainSingle();
        to.Should().Contain("active@test.com");
        cc.Should().BeEmpty();
        bcc.Should().BeEmpty();
    }

    [Fact]
    public void GetRecipientsByType_WithEmptyRecipients_ReturnsEmptyArrays()
    {
        // Arrange
        var group = new NotificationGroup
        {
            GroupName = "Test Group",
            Recipients = new List<GroupRecipient>()
        };

        // Act
        var (to, cc, bcc) = group.GetRecipientsByType();

        // Assert
        to.Should().BeEmpty();
        cc.Should().BeEmpty();
        bcc.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_ShouldInitializeDefaultValues()
    {
        // Arrange & Act
        var group = new NotificationGroup { GroupName = "Test Group" };

        // Assert
        group.Id.Should().BeEmpty();
        group.Recipients.Should().NotBeNull();
        group.Recipients.Should().BeEmpty();
        group.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        group.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }
}
