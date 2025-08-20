using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NotificationService.Application.DTOs;
using NotificationService.Application.UseCases;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces;
using NotificationService.Domain.Models;
using Xunit;

namespace NotificationService.Test.UseCases;

public class SendEmailUseCaseTests
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
            // Test-only: no consumo; devolver secuencia vacía
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
        public string Sanitize(string html) => html.Replace("<script>", "").Replace("</script>", "");
    }

    [Fact]
    public async Task Enqueues_Sanitized_And_Rendered_Html()
    {
        var queue = new FakeQueue();
        var usecase = new SendEmailUseCase(queue, new FakeRenderer(), new FakeIdempotency(), new FakeLogRepo(), new AllowAllSanitizer());

    var res = await usecase.SendAsync(new SendEmailRequestDto {
            To = "a@b.com",
            Subject = "Test",
            TemplateKey = "layouts/base",
            HtmlBody = "<div>Hola<script>alert('x')</script></div>"
        }, idempotencyKey: null, ct: CancellationToken.None);

        Assert.NotNull(queue.Last);
        Assert.Equal("queued", res.Status);
        Assert.Contains("<layout>", queue.Last!.HtmlBody);
        Assert.DoesNotContain("<script>", queue.Last!.HtmlBody);
    }
}
