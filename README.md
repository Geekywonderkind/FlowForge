# FlowForge
A system that orchestrates jobs, controls execution rates, and keeps workloads flowing reliab

### Reliable Job Orchestration & Rate Limiting

**Every job. The right time. The right pace.**

FlowForge is a lightweight job orchestration system designed to execute background tasks reliably while controlling execution rates across clients and downstream services.

The goal is to explore the engineering challenges behind distributed job processing: asynchronous execution, scheduling, concurrency, retries, rate limiting, and failure recovery.

> **Project status:** Initial development
> **Scope:** Focused V1, designed to evolve into a more capable distributed system.

---

## 1. The Problem

Applications frequently need to perform work that should not happen as part of a synchronous API request.

Examples include:

- Generating large reports.
- Sending emails and notifications.
- Processing uploaded files.
- Calling external APIs.
- Running scheduled data-processing tasks.
- Executing bulk operations for multiple customers.

A naive implementation often starts a background thread or puts tasks into an in-memory queue. This approach works for small workloads but introduces several problems as the system grows.

### Challenges

**Reliability:** What happens to queued jobs when the application restarts or a worker crashes?

**Traffic spikes:** How does the system handle thousands of jobs arriving within a short period?

**Rate limits:** How do we prevent jobs from overwhelming an external service with a limited request quota?

**Concurrency:** How do we prevent multiple workers from executing the same job simultaneously?

**Retries:** How should transient failures be retried without continuously hammering an unavailable service?

**Fairness:** How do we prevent one client from consuming all available execution capacity?

**Observability:** How can clients and operators understand whether a job is queued, running, delayed, or failing?

FlowForge aims to address these problems through a simple, modular job-processing architecture.

## 2. Project Goals

The initial version focuses on the following capabilities:

- Accept and persist asynchronous jobs.
- Execute jobs using a background worker pool.
- Support delayed and scheduled execution.
- Enforce configurable rate limits.
- Retry transient failures with exponential backoff.
- Track job status and execution attempts.
- Handle worker failures and duplicate message delivery.
- Support job cancellation before execution.
- Expose APIs for job submission and status inspection.

The system will prioritize correctness, clear design, and testability over premature optimization.

## 3. Proposed Architecture

```text
                  ┌─────────────────────┐
                  │       Client        │
                  └──────────┬──────────┘
                             │
                             ▼
                  ┌─────────────────────┐
                  │      Job API        │
                  │ Validation, Auth,   │
                  │ Idempotency         │
                  └──────────┬──────────┘
                             │
                             ▼
                  ┌─────────────────────┐
                  │   Persistent Store  │
                  │ Job State & Metadata│
                  └──────────┬──────────┘
                             │
                             ▼
                  ┌─────────────────────┐
                  │ Dispatcher/Scheduler│
                  │ Eligible Job Pickup │
                  └──────────┬──────────┘
                             │
                             ▼
                  ┌─────────────────────┐
                  │    Rate Limiter     │
                  │ Tenant & Service    │
                  │       Quotas        │
                  └──────────┬──────────┘
                             │
                             ▼
                  ┌─────────────────────┐
                  │    Durable Queue    │
                  └──────────┬──────────┘
                             │
                   ┌─────────┴─────────┐
                   ▼                   ▼
          ┌────────────────┐  ┌────────────────┐
          │   Worker 1     │  │   Worker N     │
          │ Claim & Execute│  │ Claim & Execute│
          └────────┬───────┘  └────────┬───────┘
                   └──────────┬────────┘
                              ▼
                   ┌────────────────────┐
                   │ External Services  │
                   │ / Job Handlers     │
                   └────────────────────┘
```

### Component responsibilities

| Component        | Responsibility                                                    |
| ---------------- | ----------------------------------------------------------------- |
| Job API          | Accept requests, validate inputs, and expose job operations       |
| Job Store        | Persist job state, scheduling information, and execution attempts |
| Dispatcher       | Identify eligible jobs and publish them for execution             |
| Scheduler        | Determine when delayed jobs become eligible                       |
| Rate Limiter     | Enforce execution quotas at the appropriate scope                 |
| Message Queue    | Buffer work and support redelivery                                |
| Worker Pool      | Execute jobs and report outcomes                                  |
| Recovery Process | Detect expired leases and recover abandoned jobs                  |

The architecture is intentionally modular. Components can initially run in a single application and be separated into independently scalable services later.

## 4. Basic Implementation

The initial implementation will use the following stack.

| Technology     | Purpose                                    |
| -------------- | ------------------------------------------ |
| C# / .NET      | API, orchestration logic, and workers      |
| ASP.NET Core   | REST API and hosted background services    |
| PostgreSQL     | Persistent job state and execution history |
| Redis          | Shared rate-limit state                    |
| RabbitMQ       | Asynchronous job delivery                  |
| xUnit          | Unit and integration testing               |
| Docker Compose | Local development environment              |

These are proposed implementation choices, not dependencies that must all be introduced on day one.

### 4.1 Job submission

A client submits a job through `POST /jobs`.

Example request:

```json
{
  "jobType": "REPORT_EXPORT",
  "payload": {
    "reportId": "report-123"
  },
  "scheduledAt": null,
  "idempotencyKey": "export-report-123"
}
```

The API validates the request and persists the job before reporting successful acceptance.

Example response:

```json
{
  "jobId": "job-abc123",
  "status": "QUEUED"
}
```

The client can use the returned identifier to track the job without keeping the original HTTP request open.

### 4.2 Job lifecycle

The initial lifecycle is:

```text
QUEUED
   |
   v
RUNNING ──────────────> SUCCEEDED
   |
   +── Transient failure ──> RETRY_WAIT
   |                            |
   |                            v
   |                         QUEUED
   |
   +── Attempts exhausted ──> FAILED

QUEUED ── Cancellation ──> CANCELLED
```

Jobs will have explicit state transitions so that concurrent workers cannot arbitrarily overwrite one another's results.

The first implementation will support a small set of job types through registered handlers rather than dynamically executing arbitrary code supplied by clients.

### 4.3 Worker execution

Workers consume job messages and execute the corresponding handler.

Each worker will:

1. Claim the job using an atomic state transition or lease.
2. Record the current attempt.
3. Execute the registered handler.
4. Persist the result or failure.
5. Acknowledge the queue message after the state update succeeds.

Worker leases and recovery logic will allow abandoned jobs to be retried after a crash.

The system will target **at-least-once execution**. Job handlers must therefore be idempotent where possible, because a crash can occur after a side effect succeeds but before its completion is recorded.

### 4.4 Rate limiting

FlowForge will initially implement a token-bucket rate limiter.

Each bucket has:

- A configurable refill rate.
- A maximum token capacity.
- A current token balance.
- A timestamp for the last refill.

For example, a downstream service may permit 10 requests per second with a burst capacity of 20 requests.

A job must acquire the relevant permit before it is dispatched or executed, depending on where the protected operation occurs. When no permit is available, the job is deferred instead of being executed immediately.

The design will allow limits at different scopes:

- Per tenant.
- Per job type.
- Per downstream service.
- Globally.

The shared Redis implementation will use an atomic operation to prevent concurrent dispatchers from independently exceeding the same quota.

### 4.5 Retry and recovery

Transient failures will be retried using exponential backoff with jitter.

For example:

```text
Attempt 1: Immediate execution
Attempt 2: Retry after approximately 1 second
Attempt 3: Retry after approximately 2 seconds
Attempt 4: Retry after approximately 4 seconds
```

Actual delays will be configurable and capped.

Permanent failures will not be retried unnecessarily. Jobs that exhaust their retry budget will be marked as failed or moved to a dead-letter state for investigation.

The recovery process will identify jobs abandoned by crashed workers using lease expiration and will prevent stale attempts from overwriting newer results.

## 5. API Surface

The initial API will expose the following operations.

| Method | Endpoint              | Description                |
| ------ | --------------------- | -------------------------- |
| `POST` | `/jobs`               | Submit a job               |
| `GET`  | `/jobs/{id}`          | Retrieve job status        |
| `GET`  | `/jobs`               | List jobs with filters     |
| `POST` | `/jobs/{id}/cancel`   | Cancel a queued job        |
| `GET`  | `/jobs/{id}/attempts` | Retrieve execution history |

The API will use appropriate HTTP status codes and support idempotent submission through client-provided idempotency keys.

## 6. Reliability and Design Considerations

FlowForge will explicitly address several distributed-systems trade-offs.

- **Durable acceptance:** An accepted job must survive an application restart.
- **Duplicate delivery:** Queue messages may be delivered more than once.
- **Worker crashes:** Expired leases must allow abandoned work to be recovered.
- **Database and queue consistency:** A transactional outbox or equivalent recoverable publication mechanism will prevent persisted jobs from being silently stranded.
- **Rate-limit consistency:** Shared atomic state will coordinate quotas across application instances.
- **Fair scheduling:** Per-tenant limits will prevent one customer from monopolizing execution capacity.
- **Failure isolation:** Downstream service failures should not bring down the entire worker pool.
- **Observability:** Job transitions, failures, retries, and throttling decisions should be inspectable.

The implementation will not claim universal exactly-once execution. Instead, it will use durable state, recoverable delivery, and idempotency to provide practical reliability.

## 7. Development Roadmap

### Phase 1 — Core orchestration

- [ ] Define the job model and lifecycle.
- [ ] Implement job submission and status APIs.
- [ ] Persist jobs in PostgreSQL.
- [ ] Implement a basic background worker.
- [ ] Register and execute job handlers.
- [ ] Add unit tests for state transitions.

### Phase 2 — Reliability

- [ ] Integrate RabbitMQ.
- [ ] Implement retries and exponential backoff.
- [ ] Add job attempt history.
- [ ] Implement worker leases and recovery.
- [ ] Handle duplicate delivery and idempotency.
- [ ] Address database-to-queue publication consistency.

### Phase 3 — Rate limiting and scheduling

- [ ] Implement a token-bucket algorithm.
- [ ] Store shared rate-limit state in Redis.
- [ ] Add per-tenant and per-service policies.
- [ ] Implement delayed and scheduled execution.
- [ ] Defer jobs when permits are unavailable.
- [ ] Test quota enforcement under concurrent load.

### Phase 4 — Production-minded improvements

- [ ] Add structured logging and metrics.
- [ ] Implement cancellation and dead-letter handling.
- [ ] Add priority-aware scheduling and fairness.
- [ ] Simulate worker crashes and downstream failures.
- [ ] Run load tests and measure throughput and latency.
- [ ] Document architecture decisions and trade-offs.

## 8. Success Criteria

The project will be considered successful when it can demonstrate that:

1. Jobs are persisted and survive service restarts.
2. Multiple workers can execute independent jobs concurrently.
3. Failed jobs are retried according to policy.
4. Crashed workers do not permanently strand jobs.
5. Duplicate deliveries do not cause duplicate business effects when handlers support idempotency.
6. Rate-limit enforcement remains coordinated across multiple dispatcher instances.
7. Jobs delayed by rate limits eventually execute when capacity becomes available.
8. Job state, attempts, and failure reasons can be inspected through the API.
9. Automated tests cover the critical concurrency and failure scenarios.

Performance targets will be established through load testing rather than assumed in advance.

## 9. Future Enhancements

Once the core system is reliable, potential extensions include:

- Workflow DAGs and job dependencies.
- Recurring jobs and cron-style scheduling.
- Weighted fair queuing.
- Dynamic worker scaling.
- Priority aging and starvation prevention.
- Distributed tracing.
- Tenant-specific resource budgets.
- A dashboard for monitoring jobs and queues.
- Pluggable persistence and message-broker adapters.

These are future directions rather than requirements for the first release.

---

**FlowForge — Reliable Job Orchestration & Rate Limiting**

*A focused implementation of the engineering principles behind reliable background processing.*
