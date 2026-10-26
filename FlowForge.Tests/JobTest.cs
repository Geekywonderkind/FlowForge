using FlowForge.Core.Models;

namespace FlowForge.Tests;

public class JobTests
{
    [Fact]
    public void NewJob_ShouldStartAsQueued()
    {
        var job = new Job("SendEmail", """{"to":"test@example.com"}""");

        Assert.NotEqual(Guid.Empty, job.Id);
        Assert.Equal(JobStatus.Queued, job.Status);
        Assert.Null(job.StartedAtUtc);
        Assert.Null(job.CompletedAtUtc);
    }

    [Fact]
    public void Job_ShouldTransitionToSucceeded()
    {
        var job = new Job("SendEmail", "{}");

        job.MarkAsRunning();
        job.MarkAsSucceeded();

        Assert.Equal(JobStatus.Succeeded, job.Status);
        Assert.NotNull(job.StartedAtUtc);
        Assert.NotNull(job.CompletedAtUtc);
    }

    [Fact]
    public void Job_ShouldRecordFailure()
    {
        var job = new Job("SendEmail", "{}");

        job.MarkAsRunning();
        job.MarkAsFailed("SMTP server unavailable");

        Assert.Equal(JobStatus.Failed, job.Status);
        Assert.Equal("SMTP server unavailable", job.ErrorMessage);
        Assert.NotNull(job.CompletedAtUtc);
    }

    [Fact]
    public void QueuedJob_ShouldBeCancellable()
    {
        var job = new Job("SendEmail", "{}");

        job.Cancel();

        Assert.Equal(JobStatus.Cancelled, job.Status);
        Assert.NotNull(job.CompletedAtUtc);
    }

    [Fact]
    public void SucceededJob_ShouldNotRunAgain()
    {
        var job = new Job("SendEmail", "{}");

        job.MarkAsRunning();
        job.MarkAsSucceeded();

        Assert.Throws<InvalidOperationException>(
            () => job.MarkAsRunning());
    }
}