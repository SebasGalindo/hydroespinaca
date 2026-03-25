using HydroEspinaca.Shared.Mongo.Interfaces;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Persistence.Documents;

namespace NotificationService.Infrastructure.Persistence.Mappers;

public class EmailLogMapper : IEntityMapper<EmailLog, EmailLogDocument>
{
    public EmailLog ToEntity(EmailLogDocument doc) => new()
    {
        Id = doc.Id,
        CorrelationId = doc.CorrelationId,
        To = doc.To,
        Subject = doc.Subject,
        Status = doc.Status,
        Provider = doc.Provider,
        ProviderMessageId = doc.ProviderMessageId,
        Error = doc.Error,
        CreatedAt = doc.CreatedAt,
        UpdatedAt = doc.UpdatedAt
    };

    public EmailLogDocument ToDocument(EmailLog entity) => new()
    {
        Id = entity.Id,
        CorrelationId = entity.CorrelationId,
        To = entity.To,
        Subject = entity.Subject,
        Status = entity.Status,
        Provider = entity.Provider,
        ProviderMessageId = entity.ProviderMessageId,
        Error = entity.Error,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };
}
