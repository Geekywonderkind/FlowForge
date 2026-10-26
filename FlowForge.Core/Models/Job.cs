using System;
using System.Collections.Generic;
using System.Text;

namespace FlowForge.Core.Models
{
    public class Job
    {
        #region Definations
        public Guid Id { get; private set; }
        public string JobType { get; private set; }
        public string Payload { get; private set; }
        public JobStatus Status { get; private set; }
        public DateTime CreatedAtUtc { get; private set; }
        public DateTime? StartedAtUtc { get; private set; }
        public DateTime? CompletedAtUtc { get; private set; }
        public string? ErrorMessage { get; private set; }

        #endregion Definations

        #region Constructors
        public Job(string jobType, string payload)
        {
            if (string.IsNullOrWhiteSpace(jobType)) throw new ArgumentException("Job type is required.", nameof(jobType));
            if (string.IsNullOrWhiteSpace(payload)) throw new ArgumentException("Job payload is required.", nameof(payload));
            Id = Guid.NewGuid();
            JobType = jobType;
            Payload = payload;
            Status = JobStatus.Queued;
            CreatedAtUtc = DateTime.UtcNow;
        }
        #endregion Constructors

        #region Public Method
        public void MarkAsRunning()
        {
            EnsureStatus(JobStatus.Queued);

            Status = JobStatus.Running;
            StartedAtUtc = DateTime.UtcNow;
        }

        public void MarkAsSucceeded()
        {
            EnsureStatus(JobStatus.Running);

            Status = JobStatus.Succeeded;
            CompletedAtUtc = DateTime.UtcNow;
            ErrorMessage = null;
        }

        public void MarkAsFailed(string errorMessage)
        {
            EnsureStatus(JobStatus.Running);

            Status = JobStatus.Failed;
            CompletedAtUtc = DateTime.UtcNow;
            ErrorMessage = errorMessage;
        }

        public void Cancel()
        {
            EnsureStatus(JobStatus.Queued);

            Status = JobStatus.Cancelled;
            CompletedAtUtc = DateTime.UtcNow;
        }
        #endregion Public Method

        #region Private Methods
        /// *******************************************************************
        /// EnsureStatus
        ///
        /// <summary>
        /// 
        /// </summary>
        /// <param name="expectedStatus"></param>
        /// <exception cref="InvalidOperationException"></exception>
        private void EnsureStatus(JobStatus expectedStatus)
        {
            if (Status != expectedStatus)
            {
                throw new InvalidOperationException($"Cannot perform this operation when job status is {Status}.");
            }
        }
        #endregion Private Methods
    }
}
