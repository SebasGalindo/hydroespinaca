using FluentAssertions;
using NotificationService.Domain.Persistence.Documents;

namespace NotificationService.Domain.Tests.Persistence.Documents;

public class NotificationGroupDocumentTests
{
    [Fact]
    public void NotificationGroupDocument_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var document = new NotificationGroupDocument();

        // Assert
        document.Id.Should().BeEmpty();
        document.GroupName.Should().BeEmpty();
        document.Description.Should().BeNull();
        document.Recipients.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void NotificationGroupDocument_ShouldSetProperties()
    {
        // Arrange & Act
        var document = new NotificationGroupDocument
        {
            Id = "group-123",
            GroupName = "Test Group",
            Description = "Test Description",
            Recipients = new List<GroupRecipientDocument>
            {
                new() { Email = "test@example.com", Type = "TO", IsActive = true }
            },
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        document.Id.Should().Be("group-123");
        document.GroupName.Should().Be("Test Group");
        document.Description.Should().Be("Test Description");
        document.Recipients.Should().HaveCount(1);
    }

    [Fact]
    public void SetId_ShouldUpdateIdProperty()
    {
        // Arrange
        var document = new NotificationGroupDocument
        {
            GroupName = "Test Group"
        };

        // Act
        document.SetId("new-id");

        // Assert
        document.Id.Should().Be("new-id");
    }

    [Fact]
    public void NotificationGroupDocument_ShouldSupportMultipleRecipients()
    {
        // Arrange & Act
        var document = new NotificationGroupDocument
        {
            GroupName = "Test Group",
            Recipients = new List<GroupRecipientDocument>
            {
                new() { Email = "to@example.com", Type = "TO", IsActive = true },
                new() { Email = "cc@example.com", Type = "CC", IsActive = true },
                new() { Email = "bcc@example.com", Type = "BCC", IsActive = false }
            }
        };

        // Assert
        document.Recipients.Should().HaveCount(3);
        document.Recipients.Should().Contain(r => r.Email == "to@example.com");
        document.Recipients.Should().Contain(r => r.Email == "cc@example.com");
        document.Recipients.Should().Contain(r => r.Email == "bcc@example.com");
    }
}

public class GroupRecipientDocumentTests
{
    [Fact]
    public void GroupRecipientDocument_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var document = new GroupRecipientDocument();

        // Assert
        document.Email.Should().BeEmpty();
        document.Type.Should().Be("TO");
        document.IsActive.Should().BeTrue();
    }

    [Fact]
    public void GroupRecipientDocument_ShouldSetProperties()
    {
        // Arrange & Act
        var document = new GroupRecipientDocument
        {
            Email = "test@example.com",
            Type = "CC",
            IsActive = false
        };

        // Assert
        document.Email.Should().Be("test@example.com");
        document.Type.Should().Be("CC");
        document.IsActive.Should().BeFalse();
    }

    [Theory]
    [InlineData("TO")]
    [InlineData("CC")]
    [InlineData("BCC")]
    public void GroupRecipientDocument_ShouldSupportAllRecipientTypes(string type)
    {
        // Arrange & Act
        var document = new GroupRecipientDocument
        {
            Email = "test@example.com",
            Type = type
        };

        // Assert
        document.Type.Should().Be(type);
    }
}
