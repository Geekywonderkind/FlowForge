
using FlowForge.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace FlowForge.Infrastructure.Data;

public class FlowForgeDbContext : DbContext
{
    public FlowForgeDbContext(DbContextOptions<FlowForgeDbContext> options) : base(options){}

    public DbSet<Job> Jobs => Set<Job>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var job = modelBuilder.Entity<Job>();

        job.ToTable("jobs");

        job.HasKey(j => j.Id);

        job.Property(j => j.Id)
           .HasColumnName("id");

        job.Property(j => j.JobType)
           .HasColumnName("job_type")
           .HasMaxLength(100)
           .IsRequired();

        job.Property(j => j.Payload)
           .HasColumnName("payload")
           .IsRequired();

        job.Property(j => j.Status)
           .HasColumnName("status")
           .HasConversion<string>()
           .HasMaxLength(30)
           .IsRequired();

        job.Property(j => j.CreatedAtUtc)
           .HasColumnName("created_at_utc")
           .IsRequired();

        job.Property(j => j.StartedAtUtc)
           .HasColumnName("started_at_utc");

        job.Property(j => j.CompletedAtUtc)
           .HasColumnName("completed_at_utc");

        job.Property(j => j.ErrorMessage)
           .HasColumnName("error_message");

        job.HasIndex(j => j.Status);
        job.HasIndex(j => j.CreatedAtUtc);
    }
}
