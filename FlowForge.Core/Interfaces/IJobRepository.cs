
using FlowForge.Core.Models;

namespace FlowForge.Core.Interfaces;

public interface IJobRepository
{
    Task AddAsync(Job job);

    Task<Job?> GetByIdAsync(Guid id);

    Task<IReadOnlyCollection<Job>> GetAllAsync();

    Task<IReadOnlyCollection<Job>> GetQueuedAsync();

    Task UpdateAsync(Job job);
}
