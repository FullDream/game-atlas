# Tasks

## 1. Separate durable lifecycle from transient progress

- [x] 1.1 Remove percentage mutation from persisted MapStyle state while preserving status/attempt concurrency and legal retry/publication transitions; verify focused backend domain/processing tests cover monotonic transient values, reset on retry, stale callbacks, and ready only after commit.
- [x] 1.2 Add a bounded attempt-aware runtime progress store and apply its projection to map/style list, get, create, retry, and patch responses; verify backend response tests retain existing DTO shapes, derive ready=100, use absent progress=0, and preserve timestamps on percentage changes.
- [x] 1.3 Add a forward EF migration dropping persisted Progress without editing historical migrations or enabling startup migration; verify a disposable database upgrade preserves style status/source/identity and existing ready metadata, and run the affected backend build.
- [x] 1.4 Update the Infrastructure Maps README and style contract documentation for transient percentages and durable status/errors; verify documentation agrees with the DTO fallback behavior and passes the documentation checker.

## 2. Dispatch work directly and restore it once at startup

- [x] 2.1 Introduce the minimal scheduling contracts and single-reader attempt queue, wire AddMapStyle and RetryMapStyle after successful commit, and replace RunNext polling with exact-attempt consumption; verify controlled processing tests cover post-commit dispatch, rollback, duplicate dispatch, request cancellation after commit, and one native writer.
- [x] 2.2 Add an explicit bootstrap readiness boundary and restore queued attempts once in deterministic order; verify startup/admission race tests and committed-but-undispatched recovery do not lose or duplicate accepted work.
- [x] 2.3 Replace periodic Recover with startup interruption failure, safe durable error, and transactional derived-output cleanup scheduling; verify queued work resumes, processing requires manual retry, ready styles/originals survive, and a crash after public directory move removes uncommitted output.
- [x] 2.4 Preserve guarded publication and isolate notification failures from job outcomes; verify backend fault tests show failed ready commits never advertise ready, a send failure cannot fail a committed ready style, and concurrent metadata edits survive.
- [x] 2.5 Define shutdown behavior and remove the one-second work-discovery and 30-second reconciliation loops; verify controlled shutdown never auto-requeues processing and SQL interception records no recurring work queries after bootstrap when idle.
- [x] 2.6 Update the Infrastructure Maps README for queue ownership, startup failure, readiness, manual retry, and the single-writer constraint; verify described recovery cases match the updated processing/fault tests and documentation checker.

## 3. Schedule durable cleanup without table polling

- [x] 3.1 Dispatch exact committed cleanup intent identities from style/map/game deletion and failed/interrupted processing, restoring outstanding due times once at startup; verify cascade and rollback tests retain durable intents and never schedule rolled-back deletion.
- [x] 3.2 Execute known intents with due-time retries and no periodic discovery scan, retaining the native writer boundary and stop-before-delete behavior; verify filesystem/access fault tests and restart tests preserve originals, clean derived files eventually, and record zero idle cleanup queries.
- [x] 3.3 Fence pending old cleanup against a manual retry and remove obsolete runtime jobs on deletion; verify retry waits only for its own cleanup, unrelated styles continue, old cleanup cannot remove new output, and stale completion cannot resurrect deleted resources.
- [x] 3.4 Update cleanup/storage operational documentation for retry timing and eventual removal after IO failure; verify it preserves existing static-file/cache semantics and passes the documentation checker.

## 4. Replace SSE with ordinary SignalR processing events

- [x] 4.1 Keep typed MapHub with JoinMap/LeaveMap delegating only to SignalR groups; remove MapHubConnections and custom subscription contracts; verify idempotent group membership, multi-map isolation, and zero SQL on join/leave.
- [x] 4.2 Remove MapProcessingRuntime and keep queue admission, SQLite mutation locking, and cleanup independent of events. Have handlers and MapProcessor explicitly publish StyleUpdated for committed lifecycle changes and ProcessingUpdated for percentages/deletions through the typed IHubContext adapter; verify transient percentage delivery without SQL, metadata edits, attempt identity, and full ready styles.
- [x] 4.3 Replace server initial/completion snapshots with HTTP initial/reconnect reads and client-derived completion; send deletion tombstones without enumerating connections; verify HTTP reads, deletion, and viewer disconnect without cancelling native work.
- [x] 4.4 Remove MapEventStream and the old SSE route/registration, replace affected MapEventStreamTests/MapQueryTests/MapAcceptanceTests, and update MapOpenApiTests; verify the old route returns 404, eight map REST operations remain, and static GET/HEAD/304 stays database-free.
- [x] 4.5 Update the API Maps README and HTTP examples with Hub methods/events, delta schema, safe error behavior, typed style updates, and connection lifetime; verify examples match integration tests and documentation links pass checking.

## 5. Prepare frontend HTTP data access and defer processing subscriptions

- [x] 5.1 Scaffold shared Maps data access, retain the Microsoft SignalR dependency, and configure targeted Orval Maps REST generation; verify generated exports and resolved Nx lint/typecheck targets without adding a map screen.
- [x] 5.2 Keep handwritten HTTP map query/mutation options and cache reconciliation while removing the processing client, exports/types, processing cache query/key, and automatic subscription hooks as requested by the user; verify no frontend client references remain and typecheck/lint pass.
- [x] 5.3 Retain admin proxy negotiation/WebSocket forwarding and existing HTTP/media paths; verify the proxy still has ws enabled and the production build passes.
- [x] 5.4 Document the HTTP-only consumer API and explicit deferral of frontend processing subscriptions; verify documentation links and formatting without adding or running frontend unit/E2E suites.

## 6. Verify the integrated processing lifecycle

- [x] 6.1 Build the solution and run focused backend processing, fault, lifecycle, contract, and SignalR integration suites; verify queued/processing/ready/failed transitions, restart cleanup, shared-original preservation, stale-attempt fencing, and zero recurring idle/progress SQL all pass together.
- [x] 6.2 Format affected supported sources, run resolved Nx lint targets and appropriate production builds for changed frontend projects, and record checks and limitations for the current HTTP-only frontend without running frontend unit/E2E suites.
- [x] 6.3 Validate the OpenSpec change, run node scripts/check-docs.mjs and git diff --check, and review deployment/rollback instructions against the new migration and removed SSE API; verify all planning acceptance cases have implementation or validation evidence before archive.

## Verification evidence

- Explicit responsibility split: 130 map integration tests passed. A focused regression test also confirms queue admission emits no notifications and worker lifecycle notifications run after releasing the SQLite mutation lock. The Release solution build, documentation links, strict OpenSpec validation, and whitespace checks passed; the build retained one existing nullable warning in CategoryGroupApiTests.
- Direct group notification simplification: 129 map integration tests passed. Coverage includes zero SQL for join/leave and progress, per-style deltas and deletion tombstones, HTTP initial state, multiple groups, metadata after ready, and notification-send failure isolation. The frontend client remains deferred.

- Historical typed MapHub subscription revision: 127 map integration tests passed, including multi-map group isolation, idempotent membership, full style edits after ready, and progress delivery. The focused Hub/dispatch run passed 13 tests. Release solution build passed with three existing nullable warnings; documentation and strict OpenSpec checks passed. Frontend runtime code, dependency, and proxy were unchanged.
- `dotnet build GameAtlas.slnx -c Release --no-restore`: passed. Release was used because the user's running Debug API locks its assemblies; the user's process was left running.
- Focused integration run covering Map classes and MediaAcceptanceTests: 127 passed. Map domain tests: 25 passed. After the final private-method/notification-send cleanup, 12 dispatch/Hub integration tests passed again.
- `maps-data-access:typecheck`, `maps-data-access:lint`, `admin:lint`, and `admin:build:production`: passed. The production build retains existing bundle/style warning budgets; no budget was relaxed. Frontend unit/E2E suites were not added or run.
- Historical runtime exercises, performed before the user deferred and removed the frontend processing client: real JavaScript client over WebSockets, shared consumers/maps, resubscription, API restart/recovery, connection shutdown, failed-style retry through the admin proxy, bounded initial connection failure, and first-style metadata refresh while an 8192x8192 sibling remained active. Temporary processes, fixture storage/database, and exercise files were removed.
- EF migration was generated with the CLI and validated against disposable databases; existing ready style identity/source/status/timestamps survive. The working database was not migrated. Deployment still requires stopping its old writer, backing up matching state, and explicitly applying the migration.
- OpenSpec strict validation, documentation link checks, formatting, and `git diff --check`: passed. Review found no need for a broker, multiple writers, a new map screen, or category/marker synchronization.
