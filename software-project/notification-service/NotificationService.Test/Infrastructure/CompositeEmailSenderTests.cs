using System.Threading;
using System.Threading.Tasks;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces;
using NotificationService.Infrastructure.Transport;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace NotificationService.Test.Infrastructure;

public class CompositeEmailSenderTests
{
    private class SuccessSender : IEmailSender
    {
        private readonly string _provider;
        public SuccessSender(string provider) => _provider = provider;
        public Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken ct = default)
            => Task.FromResult(new EmailSendResult(true, _provider, "id-123", null));
    }
    private class FailSender : IEmailSender
    {
        public Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken ct = default)
            => Task.FromResult(new EmailSendResult(false, null, null, "boom"));
    }

    [Fact]
    public async Task Uses_Fallback_When_Primary_Fails()
    {
        var composite = new CompositeEmailSender(new FailSender(), new SuccessSender("smtp"), NullLogger<CompositeEmailSender>.Instance);
    var res = await composite.SendAsync(new EmailMessage { CorrelationId = "c1", IdempotencyKey = "i1", To = new[]{"a@b.com"}, Subject = "s", HtmlBody = "h", TemplateKey = "t" });
        Assert.True(res.Success);
        Assert.Equal("smtp", res.Provider);
    }

    [Fact]
    public async Task Skips_Fallback_When_Primary_Succeeds()
    {
        var composite = new CompositeEmailSender(new SuccessSender("resend"), new SuccessSender("smtp"), NullLogger<CompositeEmailSender>.Instance);
    var res = await composite.SendAsync(new EmailMessage { CorrelationId = "c1", IdempotencyKey = "i1", To = new[]{"a@b.com"}, Subject = "s", HtmlBody = "h", TemplateKey = "t" });
        Assert.True(res.Success);
        Assert.Equal("resend", res.Provider);
    }
}
