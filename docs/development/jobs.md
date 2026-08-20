# Studio jobs and tooling extension guide

Use this guide for Wolverine messages, persistent import jobs, Worker handlers,
hosted services, conversion orchestration, and generated source catalogs. API
and web refactoring must not change the job lifecycle, persistence schema,
storage rules, or published outputs described here.

## Contents

- [Choose an execution model](#choose-an-execution-model)
- [Build a durable job](#build-a-durable-job)
- [Implement lifecycle and concurrency](#implement-lifecycle-and-concurrency)
- [Write handlers and route messages](#write-handlers-and-route-messages)
- [Add hosted services](#add-hosted-services)
- [Extend conversion tools and catalogs](#extend-conversion-tools-and-catalogs)
- [Test background work](#test-background-work)

## Choose an execution model

| Model | Use when | Avoid when |
| --- | --- | --- |
| Synchronous controller/repository/service call | Work is short, atomic, cancellable with the request, and its result is immediately useful | Conversion, large discovery, retries, progress, or durable audit are required |
| Durable Wolverine job | Work can outlive HTTP, needs retry/progress/history, or must survive process failure | A simple database mutation can finish in the request |
| Hosted service | Work is tied to process lifecycle, such as startup reconciliation or abandoned-job recovery | A user explicitly queues independent work or needs per-run status |

Content reconciliation and asset conversion share the universal `import_jobs`
history, lifecycle timestamps, status vocabulary, abandonment handling, and
browser query API. Preserve specialized asset work items and diagnostics when
per-file detail is required.

## Build a durable job

Implement the complete flow; a handler alone is not a job:

1. Add or extend the persisted job subtype and its metrics in
   `L2.Studio.Context`.
2. Add status, target, mode/operation, and concurrency values in
   `L2.Studio.Repositories.Interfaces.Models`.
3. Add one immutable command record per file in `L2.Studio.Messages`. Carry a
   job/work-item identifier rather than a large or mutable payload.
4. Queue the job through a repository transaction using Wolverine's EF outbox,
   so the database row and control message commit together.
5. Return `202 Accepted` with the universal job summary and status URL.
6. Route externally published control or file messages in
   `StudioMessagingConfigurationExtensions`.
7. Add a Worker handler discovered from the Worker assembly.
8. Update status, timestamps, heartbeat, counts, diagnostics, and terminal error
   state transactionally.
9. Expose progress through the universal import-job contract and browser
   utilities.
10. Cover dispatch, success, warnings, failure, conflicts, retry safety, and
    recovery in owning tests.

The queueing pattern is:

```csharp
context.ImportJobs.Add(run);
outbox.Enroll(context);
await outbox.PublishAsync(new RunThingImport(run.Id));
await outbox.SaveChangesAndFlushMessagesAsync(cancellationToken);
```

Do not publish before enrolling the same context, save the row separately from
the message, or pass filesystem paths supplied by a browser directly to a
Worker message.

## Implement lifecycle and concurrency

Use the shared lifecycle values unless a real stage requires an additional
state:

- active: `queued`, `discovering`, `running`;
- successful: `succeeded`, `succeeded_with_warnings`;
- unsuccessful terminal: `failed`, `abandoned`;
- item-level reuse where supported: `reused`.

Set `RequestedAt` while queueing. Set `StartedAt` once when work begins, update
`LastHeartbeatAt` during meaningful progress, and set `FinishedAt` for every
terminal outcome. Use the injected `TimeProvider`, not `DateTimeOffset.UtcNow`,
for job lifecycle behavior.

Enforce concurrency in the database, not only in process memory:

- use a stable version-scoped concurrency key;
- serialize the check-and-insert with a transaction/advisory lock where needed;
- back the invariant with a unique filtered index over active statuses;
- return `409 Conflict` when a related job is already active.

Handlers must be idempotent. On delivery or retry, load the job/work item,
return when it is missing or terminal, and make repeated processing converge on
the same state. Use immutable fingerprints and existing artifacts for asset
reuse. Isolate per-file failures when the run can continue; preserve a fatal run
error when it cannot.

Catch non-cancellation failures only where the handler can persist a failed
terminal state. Truncate persisted errors to the entity limit. Let cancellation
propagate so shutdown does not appear as a domain failure. Recovery services
mark stale active work abandoned using the shared timeout and status rules.

## Write handlers and route messages

Keep Wolverine handlers in `L2.Studio.Worker` and mark their owning type with
`[WolverineHandler]`. Inject factories, processors, repositories, options, and
`TimeProvider`; do not construct hosts or service providers inside a handler.

```csharp
[WolverineHandler]
public sealed class ThingImportHandler(
    IDbContextFactory<GameContentDbContext> contextFactory,
    TimeProvider timeProvider)
{
    public async Task Handle(
        ImportThings message,
        CancellationToken cancellationToken)
    {
        await using var context =
            await contextFactory.CreateDbContextAsync(cancellationToken);
        var run = await context.ContentImportRuns.SingleOrDefaultAsync(
            value => value.Id == message.RunId,
            cancellationToken);
        if (run is null || ImportJobValues.TerminalStatuses.Contains(run.Status))
            return;

        // Reconcile deterministically, update metrics and lifecycle, then save.
    }
}
```

Use the control queue for discovery, orchestration, finalization, releases,
content imports, and storage reconciliation. Use the file queue for independent
heavy file conversion. Both queues are sequential in one Worker for the current
deployment; do not assume horizontal exclusivity without a database invariant.

A handler may return a follow-up command when Wolverine should cascade to the
next local handler. Add explicit transport routing when a message is published
from another host or must be delivered through a named durable queue.

## Add hosted services

Use a hosted service only for bounded lifecycle work. Register API recovery
services in `AddStudioApiApplication` and Worker startup publishers in
`WorkerJobConfigurationExtensions`.

- Honor the host cancellation token.
- Keep startup work idempotent and safe after a crash.
- Publish durable work instead of performing heavy conversion in
  `StartAsync`.
- Use a scoped service/context for each unit of work.
- Test that the service is registered only in the intended host.

## Extend conversion tools and catalogs

- Preserve public `L2.Tools.*` project names and namespaces.
- Keep decoding/parsing in the closest tool project and orchestration in
  `L2.Studio.Services` or Worker handlers.
- Validate source, staging, generated, and public paths before reading or
  writing. Never trust browser-supplied or manifest-supplied relative paths
  without containment checks.
- Keep generated output staged outside the public tree and promote it only
  after successful validation and registration.
- Register files, hashes, dependencies, fingerprints, diagnostics, catalogs,
  and published paths together according to the asset-pipeline contract.
- Keep generated source catalogs in intentional partial files and regenerate
  them with the matching script under `server/tools`. Never edit large generated
  catalog bodies by hand.
- Treat local game clients as read-only fixtures. Do not copy original resources
  into the repository.

When a published format changes, update manifest serialization tests,
`docs/asset-pipeline.md`, and every independent browser consumer contract.

## Test background work

- Test target/version dispatch and reject unsupported combinations.
- Test import reconciliation separately for add-missing and restore-defaults
  behavior, including dependency lookups.
- Test active-job conflicts and the database model for filtered unique indexes.
- Test every terminal transition, timestamps, heartbeat, counts, warnings,
  truncated errors, and cancellation behavior.
- Test retry/idempotency by invoking reconciliation or handlers against already
  completed and partially completed state.
- Test Worker handler discovery and host-specific registrations.
- Test generated catalogs for unique identifiers, valid dependency references,
  known sentinel records, and expected counts.
- Test conversion, manifest, path containment, artifact registration, and
  diagnostics in their owning service/tool/repository suites.

Run the server unit-test and both production-host Docker targets. Also run the
web validation target when job contracts or progress UI change, and Compose
validation when messaging, storage, Worker topology, or configuration changes.
