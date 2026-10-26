using FlowForge.Core.Models;

namespace FlowForge.Core.Interfaces;

public interface IJobExecutor
{
    Task ExecuteAsync(Job job, CancellationToken cancellationToken);
}