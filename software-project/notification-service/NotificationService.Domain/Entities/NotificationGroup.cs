using HydroEspinaca.Shared.Abstractions;

namespace NotificationService.Domain.Entities;

public class NotificationGroup : IIdentifiableMutable
{
    public string Id { get; set; } = string.Empty;
    public required string GroupName { get; set; }
    public string? Description { get; set; }
    public List<GroupRecipient> Recipients { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public void UpdateTimestamp()
    {
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetId(string id)
    {
        Id = id;
    }

    public bool HasActiveRecipients()
    {
        return Recipients.Any(r => r.IsActive);
    }

    public (string[] to, string[] cc, string[] bcc) GetRecipientsByType()
    {
        var activeRecipients = Recipients.Where(r => r.IsActive).ToList();
        
        var to = activeRecipients.Where(r => r.Type == RecipientType.TO).Select(r => r.Email).ToArray();
        var cc = activeRecipients.Where(r => r.Type == RecipientType.CC).Select(r => r.Email).ToArray();
        var bcc = activeRecipients.Where(r => r.Type == RecipientType.BCC).Select(r => r.Email).ToArray();

        return (to, cc, bcc);
    }
}

public class GroupRecipient
{
    public required string Email { get; set; }
    public RecipientType Type { get; set; } = RecipientType.TO;
    public bool IsActive { get; set; } = true;
}

public enum RecipientType
{
    TO,
    CC,
    BCC
}