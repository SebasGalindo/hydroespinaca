using System;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Persistence.Documents;
using NotificationService.Infrastructure.Persistence.Mappers;
using Xunit;

namespace NotificationService.Test.Infrastructure;

public class NotificationGroupMapperTests
{
    [Fact]
    public void ToDocument_MapsRecipientTypeToString()
    {
        // Arrange
        var mapper = new NotificationGroupMapper();
        var entity = new NotificationGroup
        {
            Id = "test-id",
            GroupName = "TestGroup",
            Description = "Test description",
            Recipients = 
            [
                new GroupRecipient { Email = "test1@example.com", Type = RecipientType.TO, IsActive = true },
                new GroupRecipient { Email = "test2@example.com", Type = RecipientType.CC, IsActive = true },
                new GroupRecipient { Email = "test3@example.com", Type = RecipientType.BCC, IsActive = false }
            ],
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        var document = mapper.ToDocument(entity);

        // Assert
        Assert.Equal("TestGroup", document.GroupName);
        Assert.Equal("Test description", document.Description);
        Assert.Equal(3, document.Recipients.Count);
        
        Assert.Equal("TO", document.Recipients[0].Type);
        Assert.Equal("CC", document.Recipients[1].Type);
        Assert.Equal("BCC", document.Recipients[2].Type);
        
        Assert.Equal("test1@example.com", document.Recipients[0].Email);
        Assert.True(document.Recipients[0].IsActive);
        Assert.False(document.Recipients[2].IsActive);
    }

    [Fact]
    public void ToEntity_ParsesStringToRecipientType()
    {
        // Arrange
        var mapper = new NotificationGroupMapper();
        var document = new NotificationGroupDocument
        {
            Id = "test-id",
            GroupName = "TestGroup",
            Description = "Test description",
            Recipients = 
            [
                new GroupRecipientDocument { Email = "test1@example.com", Type = "TO", IsActive = true },
                new GroupRecipientDocument { Email = "test2@example.com", Type = "CC", IsActive = true },
                new GroupRecipientDocument { Email = "test3@example.com", Type = "BCC", IsActive = false },
                new GroupRecipientDocument { Email = "test4@example.com", Type = "INVALID", IsActive = true } // Should default to TO
            ],
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        var entity = mapper.ToEntity(document);

        // Assert
        Assert.Equal("TestGroup", entity.GroupName);
        Assert.Equal("Test description", entity.Description);
        Assert.Equal(4, entity.Recipients.Count);
        
        Assert.Equal(RecipientType.TO, entity.Recipients[0].Type);
        Assert.Equal(RecipientType.CC, entity.Recipients[1].Type);
        Assert.Equal(RecipientType.BCC, entity.Recipients[2].Type);
        Assert.Equal(RecipientType.TO, entity.Recipients[3].Type); // Invalid type defaults to TO
        
        Assert.Equal("test1@example.com", entity.Recipients[0].Email);
        Assert.True(entity.Recipients[0].IsActive);
        Assert.False(entity.Recipients[2].IsActive);
    }

    [Fact]
    public void RoundTrip_PreservesData()
    {
        // Arrange
        var mapper = new NotificationGroupMapper();
        var originalEntity = new NotificationGroup
        {
            Id = "test-id",
            GroupName = "TestGroup",
            Description = "Test description",
            Recipients = 
            [
                new GroupRecipient { Email = "test1@example.com", Type = RecipientType.TO, IsActive = true },
                new GroupRecipient { Email = "test2@example.com", Type = RecipientType.CC, IsActive = false }
            ],
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        var document = mapper.ToDocument(originalEntity);
        var roundTripEntity = mapper.ToEntity(document);

        // Assert
        Assert.Equal(originalEntity.Id, roundTripEntity.Id);
        Assert.Equal(originalEntity.GroupName, roundTripEntity.GroupName);
        Assert.Equal(originalEntity.Description, roundTripEntity.Description);
        Assert.Equal(originalEntity.Recipients.Count, roundTripEntity.Recipients.Count);
        
        for (int i = 0; i < originalEntity.Recipients.Count; i++)
        {
            Assert.Equal(originalEntity.Recipients[i].Email, roundTripEntity.Recipients[i].Email);
            Assert.Equal(originalEntity.Recipients[i].Type, roundTripEntity.Recipients[i].Type);
            Assert.Equal(originalEntity.Recipients[i].IsActive, roundTripEntity.Recipients[i].IsActive);
        }
    }
}