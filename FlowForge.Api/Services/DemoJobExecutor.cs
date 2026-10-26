using FlowForge.Core.Interfaces;
using FlowForge.Core.Models;

namespace FlowForge.Api.Services;

public class DemoJobExecutor : IJobExecutor
{
    private readonly ILogger<DemoJobExecutor> _logger;

    public DemoJobExecutor(ILogger<DemoJobExecutor> logger)
    {
        _logger = logger;
    }

    public async Task ExecuteAsync(Job job, CancellationToken cancellationToken)
    {
        if (job.JobType != "Echo")
        {
            throw new NotSupportedException($"Unsupported job type: {job.JobType}");
        }

        // Simulate some asynchronous work.
        await Task.Delay(500, cancellationToken);

        _logger.LogInformation( "Executed job {JobId}. Payload: {Payload}", job.Id, job.Payload);
    }
}