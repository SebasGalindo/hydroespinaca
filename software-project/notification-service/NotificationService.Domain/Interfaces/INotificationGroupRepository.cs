using NotificationService.Domain.Entities;

namespace NotificationService.Domain.Interfaces;

public interface INotificationGroupRepository
{
    Task<IEnumerable<NotificationGroup>> GetAllAsync(CancellationToken ct = default);
    Task<NotificationGroup?> GetByGroupNameAsync(string groupName, CancellationToken ct = default);
    Task<NotificationGroup> CreateAsync(NotificationGroup group, CancellationToken ct = default);
    Task<NotificationGroup?> UpdateAsync(string groupName, NotificationGroup group, CancellationToken ct = default);
    Task<bool> DeleteAsync(string groupName, CancellationToken ct = default);
    Task<bool> ExistsAsync(string groupName, CancellationToken ct = default);
}