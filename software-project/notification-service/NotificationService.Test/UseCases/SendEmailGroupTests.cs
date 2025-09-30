using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HydroEspinaca.Shared.DTOs.Notifications;
using NotificationService.Application.UseCases;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces;
using NotificationService.Domain.Models;
using Xunit;

namespace NotificationService.Test.UseCases;

public class SendEmailGroupTests
{
    private class FakeQueue : IEmailQueue
    {
        public EmailMessage? Last;
        public Task EnqueueAsync(EmailMessage message, CancellationToken ct = default)
        {
            Last = message; return Task.CompletedTask;
        }
        public IAsyncEnumerable<EmailMessage> DequeueAllAsync(CancellationToken ct = default)
        {
            return GetEmpty();
            static async IAsyncEnumerable<EmailMessage> GetEmpty()
            {
                yield break;
            }
        }
    }

    private class FakeRenderer : ITemplateRenderer
    {
        public Task<string> RenderAsync(string templateKey, string sanitizedBodyHtml, object? model = null, CancellationToken ct = default)
            => Task.FromResult($"<layout>{sanitizedBodyHtml}</layout>");
    }

    private class FakeIdempotency : IIdempotencyStore
    {
        public Task<(bool acquired, IdempotencyRecord record)> TryReserveAsync(string key, string correlationId, TimeSpan ttl, CancellationToken ct)
            => Task.FromResult((true, new IdempotencyRecord {
                Key = key,
                CorrelationId = correlationId,
                CreatedAt = DateTimeOffset.UtcNow,
                ExpiresAt = DateTimeOffset.UtcNow.Add(ttl),
                Status = IdempotencyStatus.Reserved
            }));
        public Task<IdempotencyRecord?> GetAsync(string key, CancellationToken ct = default)
            => Task.FromResult<IdempotencyRecord?>(null);
        public Task UpdateAsync(string key, Action<IdempotencyRecord> update, CancellationToken ct)
        {
            var rec = new IdempotencyRecord
            {
                Key = key,
                CorrelationId = Guid.NewGuid().ToString("N"),
                CreatedAt = DateTimeOffset.UtcNow,
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
                Status = IdempotencyStatus.Reserved
            };
            update(rec);
            return Task.CompletedTask;
        }
    }

    private class FakeLogRepo : IEmailLogRepository
    {
        public Task InsertAsync(EmailLog log, CancellationToken ct) => Task.CompletedTask;
        public Task UpdateStatusAsync(string correlationId, EmailDeliveryStatus status, string? provider, string? providerMessageId, string? error, CancellationToken ct) => Task.CompletedTask;
    }

    private class AllowAllSanitizer : ISanitizer
    {
        public string Sanitize(string html) => html;
    }

    private class MockGroupRepo : INotificationGroupRepository
    {
        private readonly Dictionary<string, NotificationGroup> _groups = new();

        public MockGroupRepo()
        {
            // Add test group
            _groups["Admins"] = new NotificationGroup
            {
                Id = "test-id",
                GroupName = "Admins",
                Description = "Admin users",
                Recipients = 
                [
                    new GroupRecipient { Email = "admin1@test.com", Type = RecipientType.TO, IsActive = true },
                    new GroupRecipient { Email = "admin2@test.com", Type = RecipientType.CC, IsActive = true },
                    new GroupRecipient { Email = "inactive@test.com", Type = RecipientType.TO, IsActive = false }
                ],
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        public Task<IEnumerable<NotificationGroup>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult(_groups.Values.AsEnumerable());
        
        public Task<NotificationGroup?> GetByGroupNameAsync(string groupName, CancellationToken ct = default)
            => Task.FromResult(_groups.TryGetValue(groupName, out var group) ? group : null);
        
        public Task<NotificationGroup> CreateAsync(NotificationGroup group, CancellationToken ct = default)
            => Task.FromResult(group);
        
        public Task<NotificationGroup?> UpdateAsync(string groupName, NotificationGroup group, CancellationToken ct = default)
            => Task.FromResult<NotificationGroup?>(group);
        
        public Task<bool> DeleteAsync(string groupName, CancellationToken ct = default)
            => Task.FromResult(false);
        
        public Task<bool> ExistsAsync(string groupName, CancellationToken ct = default)
            => Task.FromResult(_groups.ContainsKey(groupName));
    }

    [Fact]
    public async Task Group_Mode_Resolves_Recipients_Correctly()
    {
        var queue = new FakeQueue();
        var groupRepo = new MockGroupRepo();
        var usecase = new SendEmailUseCase(queue, new FakeRenderer(), new FakeIdempotency(), new FakeLogRepo(), new AllowAllSanitizer(), groupRepo);

        var res = await usecase.SendAsync(new SendEmailRequestDto {
            Group = "Admins",
            Subject = "Test Group Email",
            HtmlBody = "<div>Group message</div>"
        }, idempotencyKey: null, ct: CancellationToken.None);

        Assert.NotNull(queue.Last);
        Assert.Equal("queued", res.Status);
        
        // Verify recipients were resolved correctly
        Assert.Contains("admin1@test.com", queue.Last!.To);
        Assert.Contains("admin2@test.com", queue.Last!.Cc);
        Assert.DoesNotContain("inactive@test.com", queue.Last!.To); // Inactive should be filtered out
    }

    [Fact]
    public async Task Group_Mode_Throws_When_Group_Not_Found()
    {
        var queue = new FakeQueue();
        var groupRepo = new MockGroupRepo();
        var usecase = new SendEmailUseCase(queue, new FakeRenderer(), new FakeIdempotency(), new FakeLogRepo(), new AllowAllSanitizer(), groupRepo);

        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await usecase.SendAsync(new SendEmailRequestDto {
                Group = "NonExistentGroup",
                Subject = "Test",
                HtmlBody = "<div>Test</div>"
            }, idempotencyKey: null, ct: CancellationToken.None);
        });
    }

    [Fact]
    public async Task Cannot_Specify_Both_Group_And_To()
    {
        var queue = new FakeQueue();
        var groupRepo = new MockGroupRepo();
        var usecase = new SendEmailUseCase(queue, new FakeRenderer(), new FakeIdempotency(), new FakeLogRepo(), new AllowAllSanitizer(), groupRepo);

        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await usecase.SendAsync(new SendEmailRequestDto {
                Group = "Admins",
                To = "test@example.com",
                Subject = "Test",
                HtmlBody = "<div>Test</div>"
            }, idempotencyKey: null, ct: CancellationToken.None);
        });
    }

    [Fact]
    public async Task Must_Specify_Either_Group_Or_To()
    {
        var queue = new FakeQueue();
        var groupRepo = new MockGroupRepo();
        var usecase = new SendEmailUseCase(queue, new FakeRenderer(), new FakeIdempotency(), new FakeLogRepo(), new AllowAllSanitizer(), groupRepo);

        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await usecase.SendAsync(new SendEmailRequestDto {
                Subject = "Test",
                HtmlBody = "<div>Test</div>"
            }, idempotencyKey: null, ct: CancellationToken.None);
        });
    }
}