using FlowForge.Core.Interfaces;
using FlowForge.Core.Models;

namespace FlowForge.Api.Workers;

public class JobWorker : BackgroundService
{
    private readonly IJobQueue _queue;
    private readonly IJobRepository _repository;
    private readonly IJobExecutor _executor;
    private readonly ILogger<JobWorker> _logger;

    public JobWorker(IJobQueue queue, IJobRepository repository, IJobExecutor executor, ILogger<JobWorker> logger)
    {
        _queue = queue;
        _repository = repository;
        _executor = executor;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("FlowForge worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            Guid jobId;

            try
            {
                jobId = await _queue.DequeueAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            var job = await _repository.GetByIdAsync(jobId);

            if (job is null || job.Status != JobStatus.Queued)
            {
                continue;
            }

            try
            {
                job.MarkAsRunning();

                _logger.LogInformation("Starting job {JobId} of type {JobType}",
                                       job.Id,
                                       job.JobType);

                await _executor.ExecuteAsync(job, stoppingToken);

                job.MarkAsSucceeded();

                _logger.LogInformation("Job {JobId} succeeded.", job.Id);
            }
            catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
            {
                // The application is shutting down.
                // This in-memory job will not survive a restart.
                break;
            }
            catch (Exception ex)
            {
                job.MarkAsFailed(ex.Message);

                _logger.LogError(ex, "Job {JobId} failed.", job.Id);
            }
        }

        _logger.LogInformation("FlowForge worker stopped.");
    }
}