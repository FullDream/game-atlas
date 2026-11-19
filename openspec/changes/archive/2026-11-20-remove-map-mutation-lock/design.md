# Design

## Context

See `proposal.md` for motivation. Currently, nine command handlers (`CreateMapHandler`, `UpdateMapHandler`, `PublishMapHandler`, `DeleteMapHandler`, `AddMapStyleHandler`, `UpdateMapStyleHandler`, `RetryMapStyleHandler`, `DeleteMapStyleHandler`, `DeleteGameHandler`) and `MapProcessor` (`RecoverAsync`, `ClaimAsync`, `PublishAsync`, `FailAsync`) acquire an in-memory `IMapMutationLock` (`SemaphoreSlim(1, 1)`) around their database transactions and release it before sending SignalR notifications (`IMapNotifications`).

With PostgreSQL (`Npgsql.EntityFrameworkCore.PostgreSQL`), transactions run under MVCC and row-level locking:
- `MapStyle` already configures `AttemptId` and `Status` as optimistic concurrency tokens (`IsConcurrencyToken()`), and `(MapId, NormalizedName)` and `(MapId, CreationSequence)` have unique database indexes.
- However, operations that read a `Map` aggregate and its `Styles` to mutate `Map`-level state (`LastCreationSequence`, `Width`/`Height`, `DefaultStyleId`, `DisplayOrder`, or `Status`)—specifically `AddMapStyleHandler`, `DeleteMapStyleHandler`, `UpdateMapHandler`, `PublishMapHandler`, `UpdateMapStyleHandler`, and `MapProcessor.PublishAsync`—can race on the same map if two transactions read the `Map` row concurrently under `READ COMMITTED`.

## Goals / Non-Goals

**Goals:**
- Completely remove `IMapMutationLock` and `MapMutationLock`.
- Serialize concurrent aggregate mutations targeting the *same* `Map` row in PostgreSQL using transactional row-level locking (`SELECT ... FOR UPDATE`), without blocking operations on other maps.
- Keep `GameAtlas.Application` free of direct Npgsql package references.
- Guarantee that SignalR notifications (`IMapNotifications`) and queue dispatches happen only after the database transaction commits.

**Non-Goals:**
- Changing the database schema or adding new EF Core migrations.
- Modifying `MapWorkQueue` or `MapProcessor`'s own single-worker loop (`writer` semaphore in `MapProcessor` that serializes local background tile generation and local cleanup).

## Decisions

### 1. Per-map row locking via `SELECT ... FOR UPDATE` on `Maps`

**Decision:** When a transaction mutates an existing `Map` aggregate (`AddMapStyleHandler`, `DeleteMapStyleHandler`, `UpdateMapHandler`, `UpdateMapStyleHandler`, `PublishMapHandler`, and `MapProcessor.PublishAsync`), acquire a PostgreSQL row-level `FOR UPDATE` lock on the target `Maps` row inside the open transaction before reading/mutating the map and its styles.

To preserve Clean Architecture boundaries (where `GameAtlas.Application` references only `Microsoft.EntityFrameworkCore` and not relational/Npgsql packages), expose a focused method on `IApplicationDbContext` (or load helper implemented in `ApplicationDbContext`), such as:
```csharp
Task<Map?> LockMapWithStylesAsync(Guid gameId, Guid mapId, CancellationToken cancellationToken = default);
```
or a row-lock method `Task<bool> LockMapAsync(Guid mapId, CancellationToken cancellationToken = default)` (or `LockMapAsync(Guid gameId, Guid mapId, ...)`), executed via `Database.ExecuteSqlInterpolatedAsync` / `FromSql` in `ApplicationDbContext`:
```sql
SELECT 1 FROM "Maps" WHERE "GameId" = {gameId} AND "Id" = {mapId} FOR UPDATE
```
followed by loading the tracked `Map` with `WithStyles()`.

**Rationale:**
- **Zero false conflicts:** Unlike optimistic concurrency (`xmin`), where a background worker finishing tile generation (`PublishAsync`) at the exact moment an admin renames the map or adds a second style would throw `DbUpdateConcurrencyException`, `FOR UPDATE` makes the second transaction wait ~1–2 ms for the first transaction to commit and then read the fresh `Map` and `MapStyles` state.
- **Split-query compatibility:** `ApplicationDbContext` uses `QuerySplittingBehavior.SplitQuery`. Executing `FOR UPDATE` on the `"Maps"` row first ensures that both the subsequent `"Maps"` and `"MapStyles"` split queries see the committed state of any prior transaction on that map.
- **No schema migration needed:** Row locking is purely transactional SQL.

**Alternatives considered:**
- *Optimistic concurrency via PostgreSQL `xmin` (`IsRowVersion()`):* Rejected because concurrent admin actions and background worker completion on the same map would fail with concurrency exceptions unless wrapped in complex retry loops with `ChangeTracker` resets.
- *Keeping an in-memory per-map `ConcurrentDictionary<Guid, SemaphoreSlim>`:* Rejected because it leaks memory or requires reference-counted eviction, does not work across multiple processes, and duplicates PostgreSQL's built-in row lock manager.

### 2. Operations that do not need explicit `FOR UPDATE`

- **`CreateMapHandler`:** Inserts a new `Map` row. Uniqueness of `(GameId, Slug)` and `(GameId, NormalizedName)` is enforced by PostgreSQL unique indexes and `IPersistenceFailureClassifier`.
- **`DeleteMapHandler` and `DeleteGameHandler`:** Execute inside a transaction that records `MapCleanupIntent` and runs `ExecuteDeleteAsync`. PostgreSQL automatically acquires row locks during `DELETE`. If `MapProcessor.PublishAsync` races with deletion, `PublishAsync` locks/queries the map or style after commit, sees that the style is gone, and schedules a cleanup intent.
- **`RetryMapStyleHandler`, `MapProcessor.ClaimAsync`, `MapProcessor.FailAsync`, `MapProcessor.RecoverAsync`:** Mutate `MapStyle` rows directly. `MapStyle` has concurrency tokens on `AttemptId` and `Status`, preventing duplicate claims or stale transitions.

## Risks / Trade-offs

- **[Risk] Deadlock if lock acquisition order differs across handlers** → **Mitigation:** Always lock the parent `"Maps"` row first at the start of the transaction before querying or updating child `"MapStyles"` or content tables.
- **[Risk] Holding a database row lock while awaiting SignalR network I/O** → **Mitigation:** Always `await transaction.CommitAsync(cancellationToken)` (which releases all PostgreSQL row locks) *before* calling `IMapNotifications`. Verify this in `MapDispatchTests` by querying committed database state from an independent scope inside the notification callback.
