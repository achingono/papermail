using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Papermail.Data.Services;
using System.Threading.Channels;

namespace Papermail.Web.Services;

internal sealed class EmailPrefetchBackgroundService : BackgroundService
{
    private readonly ILogger<EmailPrefetchBackgroundService> _logger;
    private readonly IServiceProvider _services;
    private readonly IEmailPrefetchQueue _queue;

    public EmailPrefetchBackgroundService(ILogger<EmailPrefetchBackgroundService> logger, IServiceProvider services, IEmailPrefetchQueue queue)
    {
        _logger = logger;
        _services = services;
        _queue = queue;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Email prefetch background service started");
        var reader = ((EmailPrefetchQueue)_queue).Reader; // safe cast for internal impl

        while (!stoppingToken.IsCancellationRequested)
        {
            PrefetchRequest request;
            try
            {
                request = await reader.ReadAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            try
            {
                using var scope = _services.CreateScope();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                for (int i = 0; i < request.PageCount; i++)
                {
                    var page = request.StartPage + i;
                    switch (request.Folder.ToLowerInvariant())
                    {
                        case "inbox":
                            await emailService.GetInboxAsync(request.UserId, page, request.PageSize);
                            break;
                        case "sent":
                            await emailService.GetSentAsync(request.UserId, page, request.PageSize);
                            break;
                        case "drafts":
                            await emailService.GetDraftsAsync(request.UserId, page, request.PageSize);
                            break;
                        case "deleted":
                            await emailService.GetDeletedAsync(request.UserId, page, request.PageSize);
                            break;
                        case "archive":
                            await emailService.GetArchiveAsync(request.UserId, page, request.PageSize);
                            break;
                        case "junk":
                            await emailService.GetJunkAsync(request.UserId, page, request.PageSize);
                            break;
                        default:
                            _logger.LogDebug("Unknown folder {Folder} for prefetch", request.Folder);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error during email prefetch");
            }
        }

        _logger.LogInformation("Email prefetch background service stopping");
    }
}