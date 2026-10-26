
using FlowForge.Core.Interfaces;
using FlowForge.Core.Models;
using FlowForge.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FlowForge.Infrastructure.Repositories;

public class PostgresJobRepository : IJobRepository
{
    private readonly IDbContextFactory<FlowForgeDbContext> _factory;

    public PostgresJobRepository(
        IDbContextFactory<FlowForgeDbContext> factory)
    {
        _factory = factory;
    }

    public async Task AddAsync(Job job)
    {
        await using var db = await _factory.CreateDbContextAsync();

        db.Jobs.Add(job);
        await db.SaveChangesAsync();
    }

    public async Task<Job?> GetByIdAsync(Guid id)
    {
        await using var db = await _factory.CreateDbContextAsync();

        return await db.Jobs
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == id);
    }

    public async Task<IReadOnlyCollection<Job>> GetAllAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();

        return await db.Jobs
            .AsNoTracking()
            .OrderByDescending(j => j.CreatedAtUtc)
            .ToListAsync();
    }

    public async Task<IReadOnlyCollection<Job>> GetQueuedAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();

        return await db.Jobs
            .AsNoTracking()
            .Where(j => j.Status == JobStatus.Queued)
            .OrderBy(j => j.CreatedAtUtc)
            .ToListAsync();
    }

    public async Task UpdateAsync(Job job)
    {
        await using var db = await _factory.CreateDbContextAsync();

        db.Jobs.Update(job);
        await db.SaveChangesAsync();
    }
}
