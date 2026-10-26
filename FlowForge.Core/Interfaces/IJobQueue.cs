namespace FlowForge.Core.Interfaces;

public interface IJobQueue
{
    Task EnqueueAsync(Guid jobId);

    ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken);
}