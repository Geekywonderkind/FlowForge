using System.Collections.Concurrent;
using FlowForge.Core.Interfaces;
using FlowForge.Core.Models;

namespace FlowForge.Infrastructure.Repositories;

public class InMemoryJobRepository : IJobRepository
{
    private readonly ConcurrentDictionary<Guid, Job> _jobs = new();

    public Task AddAsync(Job job)
    {
        if (!_jobs.TryAdd(job.Id, job))
        {
            throw new InvalidOperationException($"A job with ID {job.Id} already exists.");
        }

        return Task.CompletedTask;
    }

    public Task<Job?> GetByIdAsync(Guid id)
    {
        _jobs.TryGetValue(id, out var job);

        return Task.FromResult(job);
    }

    public Task<IReadOnlyCollection<Job>> GetAllAsync()
    {
        IReadOnlyCollection<Job> jobs = _jobs.Values.ToArray();

        return Task.FromResult(jobs);
    }

    public Task<IReadOnlyCollection<Job>> GetQueuedAsync()
    {
        throw new NotImplementedException();
    }

    public Task UpdateAsync(Job job)
    {
        throw new NotImplementedException();
    }
}