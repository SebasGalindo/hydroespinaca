using HydroEspinaca.Shared.Mongo.Interfaces;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Persistence.Documents;

namespace NotificationService.Infrastructure.Persistence.Mappers;

public class NotificationGroupMapper : IEntityMapper<NotificationGroup, NotificationGroupDocument>
{
    public NotificationGroup ToEntity(NotificationGroupDocument d) => new()
    {
        Id = d.Id,
        GroupName = d.GroupName,
        Description = d.Description,
        Recipients = d.Recipients.Select(ToRecipientEntity).ToList(),
        CreatedAt = d.CreatedAt,
        UpdatedAt = d.UpdatedAt
    };

    public NotificationGroupDocument ToDocument(NotificationGroup e) => new()
    {
        Id = e.Id,
        GroupName = e.GroupName,
        Description = e.Description,
        Recipients = e.Recipients.Select(ToRecipientDocument).ToList(),
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt
    };

    private static GroupRecipient ToRecipientEntity(GroupRecipientDocument d) => new()
    {
        Email = d.Email,
        Type = ParseRecipientType(d.Type),
        IsActive = d.IsActive
    };

    private static GroupRecipientDocument ToRecipientDocument(GroupRecipient e) => new()
    {
        Email = e.Email,
        Type = e.Type.ToString(),
        IsActive = e.IsActive
    };

    private static RecipientType ParseRecipientType(string type) => type?.ToUpper() switch
    {
        "TO" => RecipientType.TO,
        "CC" => RecipientType.CC,
        "BCC" => RecipientType.BCC,
        _ => RecipientType.TO // Default fallback
    };
}