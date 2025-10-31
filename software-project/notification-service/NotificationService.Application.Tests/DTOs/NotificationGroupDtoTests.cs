using FluentAssertions;
using NotificationService.Application.DTOs;

namespace NotificationService.Application.Tests.DTOs;

public class NotificationGroupDtoTests
{
    [Fact]
    public void NotificationGroupDto_ShouldInitializeWithRequiredProperties()
    {
        // Arrange
        var createdAt = DateTime.UtcNow;
        var updatedAt = DateTime.UtcNow;

        // Act
        var dto = new NotificationGroupDto
        {
            Id = "group-123",
            GroupName = "Test Group",
            Description = "Test Description",
            Recipients = new List<GroupRecipientDto>
            {
                new() { Email = "test@example.com", Type = RecipientTypeDto.TO, IsActive = true }
            },
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };

        // Assert
        dto.Id.Should().Be("group-123");
        dto.GroupName.Should().Be("Test Group");
        dto.Description.Should().Be("Test Description");
        dto.Recipients.Should().HaveCount(1);
        dto.CreatedAt.Should().Be(createdAt);
        dto.UpdatedAt.Should().Be(updatedAt);
    }

    [Fact]
    public void NotificationGroupDto_WithNullDescription_ShouldBeValid()
    {
        // Arrange & Act
        var dto = new NotificationGroupDto
        {
            Id = "group-123",
            GroupName = "Test Group",
            Description = null,
            Recipients = new List<GroupRecipientDto>(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        dto.Description.Should().BeNull();
    }

    [Fact]
    public void NotificationGroupDto_Recipients_ShouldDefaultToEmptyList()
    {
        // Arrange & Act
        var dto = new NotificationGroupDto
        {
            Id = "group-123",
            GroupName = "Test Group",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        dto.Recipients.Should().NotBeNull();
        dto.Recipients.Should().BeEmpty();
    }

    [Fact]
    public void NotificationGroupDto_WithMultipleRecipients_ShouldStoreAll()
    {
        // Arrange & Act
        var dto = new NotificationGroupDto
        {
            Id = "group-123",
            GroupName = "Test Group",
            Recipients = new List<GroupRecipientDto>
            {
                new() { Email = "to@example.com", Type = RecipientTypeDto.TO, IsActive = true },
                new() { Email = "cc@example.com", Type = RecipientTypeDto.CC, IsActive = true },
                new() { Email = "bcc@example.com", Type = RecipientTypeDto.BCC, IsActive = false }
            },
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        dto.Recipients.Should().HaveCount(3);
    }
}

public class CreateNotificationGroupDtoTests
{
    [Fact]
    public void CreateNotificationGroupDto_ShouldInitializeWithRequiredProperties()
    {
        // Arrange & Act
        var dto = new CreateNotificationGroupDto
        {
            GroupName = "New Group",
            Description = "New Description",
            Recipients = new List<GroupRecipientDto>
            {
                new() { Email = "test@example.com", Type = RecipientTypeDto.TO, IsActive = true }
            }
        };

        // Assert
        dto.GroupName.Should().Be("New Group");
        dto.Description.Should().Be("New Description");
        dto.Recipients.Should().HaveCount(1);
    }

    [Fact]
    public void CreateNotificationGroupDto_WithNullDescription_ShouldBeValid()
    {
        // Arrange & Act
        var dto = new CreateNotificationGroupDto
        {
            GroupName = "New Group",
            Description = null,
            Recipients = new List<GroupRecipientDto>()
        };

        // Assert
        dto.Description.Should().BeNull();
    }

    [Fact]
    public void CreateNotificationGroupDto_Recipients_ShouldDefaultToEmptyList()
    {
        // Arrange & Act
        var dto = new CreateNotificationGroupDto
        {
            GroupName = "New Group"
        };

        // Assert
        dto.Recipients.Should().NotBeNull();
        dto.Recipients.Should().BeEmpty();
    }
}

public class UpdateNotificationGroupDtoTests
{
    [Fact]
    public void UpdateNotificationGroupDto_ShouldAllowNullDescription()
    {
        // Arrange & Act
        var dto = new UpdateNotificationGroupDto
        {
            Description = null,
            Recipients = null
        };

        // Assert
        dto.Description.Should().BeNull();
    }

    [Fact]
    public void UpdateNotificationGroupDto_ShouldAllowNullRecipients()
    {
        // Arrange & Act
        var dto = new UpdateNotificationGroupDto
        {
            Description = "Updated",
            Recipients = null
        };

        // Assert
        dto.Recipients.Should().BeNull();
    }

    [Fact]
    public void UpdateNotificationGroupDto_WithDescription_ShouldStoreValue()
    {
        // Arrange & Act
        var dto = new UpdateNotificationGroupDto
        {
            Description = "Updated Description",
            Recipients = new List<GroupRecipientDto>()
        };

        // Assert
        dto.Description.Should().Be("Updated Description");
    }

    [Fact]
    public void UpdateNotificationGroupDto_WithRecipients_ShouldStoreList()
    {
        // Arrange & Act
        var dto = new UpdateNotificationGroupDto
        {
            Recipients = new List<GroupRecipientDto>
            {
                new() { Email = "updated@example.com", Type = RecipientTypeDto.TO, IsActive = true }
            }
        };

        // Assert
        dto.Recipients.Should().HaveCount(1);
        dto.Recipients!.First().Email.Should().Be("updated@example.com");
    }
}

public class GroupRecipientDtoTests
{
    [Fact]
    public void GroupRecipientDto_ShouldInitializeWithRequiredProperties()
    {
        // Arrange & Act
        var dto = new GroupRecipientDto
        {
            Email = "test@example.com",
            Type = RecipientTypeDto.CC,
            IsActive = false
        };

        // Assert
        dto.Email.Should().Be("test@example.com");
        dto.Type.Should().Be(RecipientTypeDto.CC);
        dto.IsActive.Should().BeFalse();
    }

    [Fact]
    public void GroupRecipientDto_Type_ShouldDefaultToTO()
    {
        // Arrange & Act
        var dto = new GroupRecipientDto
        {
            Email = "test@example.com"
        };

        // Assert
        dto.Type.Should().Be(RecipientTypeDto.TO);
    }

    [Fact]
    public void GroupRecipientDto_IsActive_ShouldDefaultToTrue()
    {
        // Arrange & Act
        var dto = new GroupRecipientDto
        {
            Email = "test@example.com"
        };

        // Assert
        dto.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData(RecipientTypeDto.TO)]
    [InlineData(RecipientTypeDto.CC)]
    [InlineData(RecipientTypeDto.BCC)]
    public void GroupRecipientDto_ShouldSupportAllRecipientTypes(RecipientTypeDto type)
    {
        // Arrange & Act
        var dto = new GroupRecipientDto
        {
            Email = "test@example.com",
            Type = type
        };

        // Assert
        dto.Type.Should().Be(type);
    }
}

public class RecipientTypeDtoTests
{
    [Fact]
    public void RecipientTypeDto_ShouldHaveCorrectEnumValues()
    {
        // Assert
        RecipientTypeDto.TO.Should().Be(RecipientTypeDto.TO);
        RecipientTypeDto.CC.Should().Be(RecipientTypeDto.CC);
        RecipientTypeDto.BCC.Should().Be(RecipientTypeDto.BCC);
    }

    [Fact]
    public void RecipientTypeDto_ShouldHaveThreeValues()
    {
        // Arrange & Act
        var values = Enum.GetValues<RecipientTypeDto>();

        // Assert
        values.Should().HaveCount(3);
    }

    [Theory]
    [InlineData(RecipientTypeDto.TO, 0)]
    [InlineData(RecipientTypeDto.CC, 1)]
    [InlineData(RecipientTypeDto.BCC, 2)]
    public void RecipientTypeDto_ShouldHaveCorrectUnderlyingValues(RecipientTypeDto type, int expectedValue)
    {
        // Assert
        ((int)type).Should().Be(expectedValue);
    }
}
