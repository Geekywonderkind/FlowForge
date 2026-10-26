using System.Threading.Channels;
using FlowForge.Core.Interfaces;

namespace FlowForge.Infrastructure.Queues;

public class InMemoryJobQueue : IJobQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>();

    public async Task EnqueueAsync(Guid jobId)
    {
        await _channel.Writer.WriteAsync(jobId);
    }

    public ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAsync(cancellationToken);
    }
}