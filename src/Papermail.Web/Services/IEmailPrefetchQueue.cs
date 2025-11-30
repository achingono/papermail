using System.Threading.Channels;

namespace Papermail.Web.Services;

public interface IEmailPrefetchQueue
{
    void Enqueue(string userId, string folder, int startPage, int pageCount, int pageSize);
}

internal sealed class EmailPrefetchQueue : IEmailPrefetchQueue
{
    private readonly Channel<PrefetchRequest> _channel = Channel.CreateUnbounded<PrefetchRequest>();

    public ChannelReader<PrefetchRequest> Reader => _channel.Reader;

    public void Enqueue(string userId, string folder, int startPage, int pageCount, int pageSize)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(folder)) return;
        if (!_channel.Writer.TryWrite(new PrefetchRequest(userId, folder, startPage, pageCount, pageSize)))
        {
            // drop silently if queue full/unavailable
        }
    }
}

internal readonly record struct PrefetchRequest(string UserId, string Folder, int StartPage, int PageCount, int PageSize);