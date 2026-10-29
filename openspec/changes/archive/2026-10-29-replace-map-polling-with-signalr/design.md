# Design

## Context

See [proposal.md](proposal.md) for motivation and scope and [the maps delta](specs/maps/spec.md) for acceptance behavior.

MapProcessingService currently calls RunNextAsync and CleanupAsync every second and RecoverAsync every 30 seconds. MapProcessor claims the oldest queued row, saves percentage changes in fresh contexts, validates a pyramid, moves it atomically, and guards its ready commit with attempt/status concurrency tokens. Recover currently resets processing to queued. MapEventStream rereads complete map/style/source graphs independently for each viewer every second.

MapCleanupIntent already survives map/game cascades and filesystem failures. Preserve that durability and the existing one-writer storage guard. Originals belong to shared media; only map work and tiles are cleanup targets. No frontend Maps data-access library or SSE consumer currently exists. The admin proxy currently needs explicit verification for SignalR negotiation and WebSocket upgrades.

This design is for one API process owning a database/storage pair. Its in-memory state is not a distributed queue or a multi-instance notification mechanism.

## Goals / Non-Goals

**Goals:** Make idle processing and connected progress delivery issue zero recurring SQL; make commit, dispatch, notification, and restart boundaries explicit; keep one source of UI processing state; and bound notification backpressure and isolate delivery failures.

**Non-Goals:** Replace the native image pipeline, create map screens, synchronize arbitrary metadata, provide durable percentage history, support multiple writers, or add a broker/backplane.

## Decisions

### 1. Dispatch committed attempts to a single-reader queue

Introduce small Application-facing contracts for scheduling processing, coordinating map mutations, reading transient progress, and publishing processing changes. Implement the queue and SQLite write coordination separately in Infrastructure; keep Hub and SignalR types in Api. Domain owns legal lifecycle transitions and remains independent of transport and runtime percentages.

Use a process-local single-reader channel carrying style/attempt identity. Creation and retry commit queued state first, then synchronously register/enqueue the committed attempt before returning success. This final dispatch does not use the HTTP request cancellation token. No native work occurs in a database transaction. Claim only the identified queued attempt in a fresh short scope, persist processing, then start generation. Dedupe repeated dispatch by attempt and check persisted attempt/status before working; skip removed or obsolete items.

Queue admission ordering and startup restoration preserve creation order where applicable. The worker waits for messages rather than asking SQL whether work exists. A failure to update the runtime queue after a successful commit must not be swallowed: stop accepting affected work and recover through process restart rather than leave an indefinitely stranded row. A process crash in that gap is covered by startup restoration.

Alternative: a database queue with a wake signal would reduce idle reads, but still rediscover jobs through queries. Carrying exact identities makes dispatch explicit and deduplication testable. An external durable broker is unnecessary for the single-process scope.

### 2. Persist lifecycle, project percentages from memory

Remove persisted intermediate Progress from MapStyle through a new forward EF migration; do not rewrite historical migrations. Retain durable status, AttemptId, safe error fields, metadata, and timestamps. HTTP DTO progress remains for compatibility: queued is 0, ready is 100, and processing/failed use a matching in-memory attempt value or 0 if absent. Apply this projection consistently to list, get, creation, retry, and patch responses; it must never overlay an obsolete attempt or resurrect deleted metadata.

Native callbacks only update the thread-safe progress buffer. Keep values monotonic and clamped to 0..99 until ready commits. Percentage-only changes never touch a tracked entity or UpdatedAt. Retain the last failed percentage in a bounded transient cache; its absence or process restart yields 0. Lifecycle events are produced only after the corresponding database commit. Notification send failure cannot convert an already ready style to failed.

Build per-style processing deltas from memory, reporting percentage changes approximately once per second. Do not throttle lifecycle transitions behind that timer. Avoid application heartbeats; SignalR manages transport liveness.

Alternative: leave Progress mapped but stop updating it. Dropping the field makes the ownership rule explicit and avoids stale stored values appearing authoritative.

### 3. Recover once before accepting normal work

Startup checks native capabilities, loads unfinished styles and cleanup intents, and establishes runtime state before admitting style mutations or subscriptions. Coordinate readiness explicitly; do not depend on a background task happening to finish before the first request.

- Persist processing styles as failed with a safe interruption error and enqueue durable output cleanup in the same transaction. Do not restore them as runnable work, even if their complete pyramid already moved into public storage.
- Attempt cleanup of interrupted output before normal processing; retain failed deletions durably with due-time retries. Ready pyramids and all shared originals survive.
- Restore persisted queued attempts once, in deterministic creation order, without changing them to failed.
- Start consuming new admissions only after this bootstrap barrier, merging/deduping any accepted dispatch consistently.

Remove periodic recovery. Graceful shutdown stops admission and native writing; uncompleted processing must become failed either during shutdown when persistence is available or through startup recovery. It is never automatically requeued.

Alternative: resumable generation or automatic restart was explicitly rejected in the discussion.

### 4. Keep cleanup durable but schedule known work directly

Keep cleanup intents transactional with resource deletion or failed/interrupted status. After commit, publish exact intent identities to the scheduler. At startup, read outstanding intents once, including future RetryAt values. Hold known retries in memory and wait until the earliest due item or a new message; query/update that specific intent when executing it. When no tasks remain, wait without scanning SQL.

Serialize native writers and file deletion using the existing writer boundary. Stop and await the relevant native attempt before removing its output. Cleanup failures save their next due time and reenter the scheduler. Resource deletion captures affected map identities before cascades so runtime state, queued items, and live subscribers can be reconciled after commit.

Pending old cleanup must not erase a retry's output. Before starting a retried style, complete its previous output cleanup under the writer guard; if that cleanup is still blocked, delay only that style until its known cleanup retry succeeds. Allow unrelated styles to run. Do not leave a style in processing while it is merely waiting for cleanup. Preserve attempt fencing at publication and avoid overwriting concurrent metadata edits.

Filesystem and SQLite cannot commit atomically. Commit safe failed state and a cleanup obligation even when physical removal cannot finish immediately; log the failure and retry. If persistence itself is unavailable, fail the operation safely and retain the processing/intent state for recovery rather than pretend completion succeeded or run an unbounded discovery loop.

### 5. Send typed events directly to SignalR groups

Expose `MapHub : Hub<IMapHubClient>` at `/hubs/map`, proxied as `/api/hubs/map`. JoinMap and LeaveMap only add/remove `Context.ConnectionId` in `map:{mapId}`. SignalR owns membership and disconnect cleanup. Remove MapHubConnections, runtime subscriptions, per-map snapshot dictionaries, and notification channels. Joining performs no SQL, no existence check, and no initial delivery. Missing resources are reported by HTTP GET. There is no authorization model; membership alone does not grant or validate access.

Implement only StyleUpdated(MapStyleDto) and ProcessingUpdated(MapProcessingDto). Introduce a small Application notification interface implemented in Api through the typed IHubContext. MapProcessor and HTTP style handlers explicitly notify after commit, independent of listeners. Remove MapProcessingRuntime: IMapProcessingQueue only schedules identities and IMapMutationLock only coordinates SQLite writes. Enqueue has no notification side effects. Deletion handlers notify directly; cleanup scheduling and file retries know nothing about client events. Startup only restores queued work and does not publish initial resource events. StyleUpdated sends full styles on committed metadata/lifecycle changes. Do not duplicate those lifecycle transitions as ProcessingUpdated; that event carries percentage ticks and deletion tombstones. ProcessingUpdated is a single-style delta containing mapId, styleId, attemptId, status, progress, error, and deleted. Percentage ticks contain no full metadata. Style deletion is a tombstone for styleId; map/game deletion uses styleId:null. There are no revisions, full lists, or server completion flags. Clients derive completion from cached styles. No category, marker, or map metadata event is introduced.

Keep the short SQLite writer coordination gate, but explicitly release it before awaited notification delivery. Queue dispatch and durable cleanup scheduling remain post-commit, occur before releasing the mutation lock, and are independent of HTTP request cancellation. Notifications are sent only after the lock is released. Sends are best effort with a five-second timeout; errors are logged without failing committed operations. Native callbacks only write the existing transient progress buffer; approximately once per second the worker sends its current percentage directly. No database query is performed to build notifications. There is no background notification consumer or group-specific state. Network backpressure can delay a caller up to the bounded send timeout; durability does not depend on event delivery.

Initial state and resynchronization use existing HTTP reads. A future client registers handlers, joins, and reads HTTP state while buffering events, reconciles metadata timestamps and attempt identities, retains tombstones, and refetches on reconnect, lifecycle completion or ambiguous overlap. This is not an atomic versioned snapshot; do not promise replay or gap-free snapshot sequencing. HTTP-triggered reconciliation is event driven rather than periodic. The frontend integration remains deferred.

Completion does not end membership. Clients leave maps on deletion or when consumers disappear and stop the connection when no memberships remain. The server does not enumerate connections on resource deletion. Reconnect explicitly rejoins and refetches. The former SSE and processing hub contracts are removed.

### 6. Defer the frontend processing client

The user explicitly deferred the frontend SignalR client after the initial implementation. Retain the shared Maps HTTP data-access library, generated REST transport, handwritten HTTP query/mutation options, Microsoft SignalR JavaScript dependency, and admin WebSocket proxy forwarding. Remove the processing client, its exports/types, its unused cache key/query, and automatic subscription hooks from HTTP reads and mutations.

No frontend connection or live processing cache is installed in this change. HTTP caches continue to reconcile ordinary mutation responses. Connection reference counting, resubscription, terminal metadata refresh, and any frontend stream abstraction belong to later client work. The backend Hub, compact deltas, group membership lifecycle, and queue/recovery/cleanup behavior remain in scope.

### 7. Verify observable work and absence of idle SQL

Adapt existing MapProcessingTests, MapFaultTests, MapEventStreamTests, MapAcceptanceTests, MapQueryTests, lifecycle and OpenAPI tests to the new contract. Use controlled native work and SQL interceptors to prove idle queue/cleanup and percentage notifications issue no recurring SQL after bootstrap/subscription. Verify commits for queued, processing, ready/failed remain, and static GET/HEAD/304 stays database-free.

Exercise two subscribers, the join followed by an HTTP read, stale callbacks, bounded delivery, missing maps, deletion and cascades, commit-to-dispatch crash recovery, interruption after public move, manual retry with pending cleanup, independent failures, and client disconnect without cancelling work. Keep frontend unit/E2E work deferred; validate the remaining HTTP data access with formatting, lint, typecheck, and production builds. Earlier multi-consumer/reconnect/completion exercises describe the initial client before its explicit deferral.

## Risks / Trade-offs

- In-memory queue notifications can disappear on process crash -> durable queued rows are restored once at startup; post-commit dispatch failures cannot be silently ignored.
- Percentages disappear on restart or eviction -> status/error remain durable; failed progress falls back to 0 and ready derives 100.
- Cleanup can fail or the process can stop during cleanup -> durable intents and idempotent due-time retries; block only a conflicting retry attempt.
- Client transport failures do not prove job failure -> preserve cached data, expose connection state separately, and rejoin and obtain current state through HTTP.
- SignalR adds a dependency and connection management -> keep the Hub limited to processing events and share one connection among active consumers.
- No active connection means no immediate discovery of another user's new work -> discover through ordinary metadata refresh; document this boundary.
- Multi-process operation would invalidate runtime coordination -> retain the documented single-writer deployment constraint; a broker/backplane is a separate change.
- Removing SSE and persisted percentages breaks old transport/recovery expectations -> coordinate deployment and revise contracts/tests/docs together.

## Migration Plan

1. Implement and review the new lifecycle/dispatch/cleanup and Hub contracts together with the reusable HTTP data access; keep historical migrations intact and supply the forward migration dropping stored percentages.
2. Stop the old API writer, back up the database and matching map/media storage, and explicitly apply the EF migration. No automatic startup migration or data reset is introduced.
3. Deploy the new API; frontend processing-client integration is deferred. Configure the Hub proxy for negotiation and WebSocket upgrades, retaining supported SignalR fallback transports rather than forcing WebSockets. Startup fails interrupted processing, cleans derived output, and restores queued work.
4. Verify native processing, final cache reconciliation, restart recovery, connection release, and idle SQL behavior before normal use. Existing ready tiles remain valid and statically cached.
5. Rollback requires a matching pre-upgrade database/filesystem backup and previous application. Lost transient percentages cannot be reconstructed; do not promise an in-place rollback that recreates their history or restores removed failed output.
