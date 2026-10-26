using FlowForge.Api.Contracts;
using FlowForge.Api.Services;
using FlowForge.Api.Workers;
using FlowForge.Core.Interfaces;
using FlowForge.Core.Models;
using FlowForge.Infrastructure.Queues;
using FlowForge.Infrastructure.Repositories;
using FlowForge.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register application components.
builder.Services.AddSingleton<IJobRepository, InMemoryJobRepository>();
builder.Services.AddSingleton<IJobQueue, InMemoryJobQueue>();
builder.Services.AddSingleton<IJobExecutor, DemoJobExecutor>();

// Start the background worker with the application.
builder.Services.AddHostedService<JobWorker>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Submit a job.
app.MapPost("/jobs", async (
    CreateJobRequest request,
    IJobRepository repository,
    IJobQueue queue) =>
{
    if (string.IsNullOrWhiteSpace(request.JobType) || string.IsNullOrWhiteSpace(request.Payload))
    {
        return Results.BadRequest(new
        {
            error = "JobType and Payload are required."
        });
    }

    if (!string.Equals(request.JobType,
                       "Echo",
                       StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest(new
        {
            error = "Unsupported job type. Supported type: Echo."
        });
    }
    try
    {
        var job = new Job(request.JobType, request.Payload);

        await repository.AddAsync(job);
        await queue.EnqueueAsync(job.Id);

        return Results.Accepted(
            $"/jobs/{job.Id}",
            new
            {
                job.Id,
                job.JobType,
                job.Status,
                job.CreatedAtUtc
            });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

// Cancel a queued job.
app.MapPost("/jobs/{id:guid}/cancel", async (
    Guid id,
    IJobRepository repository) =>
{
    var job = await repository.GetByIdAsync(id);

    if (job is null)
    {
        return Results.NotFound(new
        {
            error = $"Job {id} was not found."
        });
    }

    try
    {
        job.Cancel();

        return Results.Ok(new
        {
            job.Id,
            job.Status
        });
    }
    catch (InvalidOperationException)
    {
        return Results.Conflict(new
        {
            error = $"Job cannot be cancelled because its status is {job.Status}."
        });
    }
});

// Get all jobs.
app.MapGet("/jobs", async (IJobRepository repository) =>
{
    return Results.Ok(await repository.GetAllAsync());
});

// Get one job by ID.
app.MapGet("/jobs/{id:guid}", async (
    Guid id,
    IJobRepository repository) =>
{
    var job = await repository.GetByIdAsync(id);

    return job is null
        ? Results.NotFound()
        : Results.Ok(job);
});

app.Run();