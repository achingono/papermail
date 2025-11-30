using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Moq;
using Papermail.Data.Services;
using Papermail.Web.Services;

namespace Papermail.Web.Tests;

public class EmailPrefetchBackgroundServiceTests
{
    private readonly ServiceProvider _provider;
    private readonly EmailPrefetchQueue _queue;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly EmailPrefetchBackgroundService _service;
    private readonly CancellationTokenSource _cts = new();

    public EmailPrefetchBackgroundServiceTests()
    {
        var services = new ServiceCollection();
        _emailServiceMock = new Mock<IEmailService>(MockBehavior.Strict);
        services.AddSingleton(_emailServiceMock.Object);
        services.AddLogging();
        _provider = services.BuildServiceProvider();
        _queue = new EmailPrefetchQueue();
        var logger = _provider.GetRequiredService<ILogger<EmailPrefetchBackgroundService>>();
        _service = new EmailPrefetchBackgroundService(logger, _provider, _queue);
    }

    [Fact]
    public async Task Prefetch_Inbox_Pages_AreRequested()
    {
        _emailServiceMock.Setup(s => s.GetInboxAsync("user", 0, 50)).ReturnsAsync(new List<Data.Models.EmailItemModel>()).Verifiable();
        _emailServiceMock.Setup(s => s.GetInboxAsync("user", 1, 50)).ReturnsAsync(new List<Data.Models.EmailItemModel>()).Verifiable();

        await _service.StartAsync(_cts.Token);
        _queue.Enqueue("user", "inbox", 0, 2, 50);
        await Task.Delay(150); // allow background processing
        _cts.Cancel();
        await _service.StopAsync(CancellationToken.None);

        _emailServiceMock.Verify(s => s.GetInboxAsync("user", 0, 50), Times.Once);
        _emailServiceMock.Verify(s => s.GetInboxAsync("user", 1, 50), Times.Once);
    }

    [Fact]
    public async Task Prefetch_UnknownFolder_IsIgnored()
    {
        await _service.StartAsync(_cts.Token);
        _queue.Enqueue("user", "unknown-folder", 0, 1, 50);
        await Task.Delay(100);
        _cts.Cancel();
        await _service.StopAsync(CancellationToken.None);

        _emailServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task MultipleEnqueue_ProcessesAll()
    {
        _emailServiceMock.Setup(s => s.GetSentAsync("user", 3, 25)).ReturnsAsync(new List<Data.Models.EmailItemModel>()).Verifiable();
        _emailServiceMock.Setup(s => s.GetSentAsync("user", 4, 25)).ReturnsAsync(new List<Data.Models.EmailItemModel>()).Verifiable();
        _emailServiceMock.Setup(s => s.GetSentAsync("user", 5, 25)).ReturnsAsync(new List<Data.Models.EmailItemModel>()).Verifiable();

        await _service.StartAsync(_cts.Token);
        _queue.Enqueue("user", "sent", 3, 2, 25);
        _queue.Enqueue("user", "sent", 5, 1, 25);
        await Task.Delay(200);
        _cts.Cancel();
        await _service.StopAsync(CancellationToken.None);

        _emailServiceMock.Verify(s => s.GetSentAsync("user", 3, 25), Times.Once);
        _emailServiceMock.Verify(s => s.GetSentAsync("user", 4, 25), Times.Once);
        _emailServiceMock.Verify(s => s.GetSentAsync("user", 5, 25), Times.Once);
    }
}