# Proposal

## Why

`IMapMutationLock` and `MapMutationLock` were introduced when the backend used SQLite, which allows only a single concurrent writer and needed an in-memory `SemaphoreSlim(1, 1)` to serialize all map mutations across the entire process. Now that the backend uses PostgreSQL with MVCC and row-level locking, the process-wide in-memory semaphore is an unnecessary abstraction that serializes unrelated maps and cannot coordinate across process boundaries.

## What Changes

- Remove `IMapMutationLock` from `GameAtlas.Application` and `MapMutationLock` from `GameAtlas.Infrastructure`, along with its DI registration.
- Remove `IMapMutationLock` dependencies from map and game mutation handlers (`CreateMapHandler`, `UpdateMapHandler`, `PublishMapHandler`, `DeleteMapHandler`, `AddMapStyleHandler`, `UpdateMapStyleHandler`, `RetryMapStyleHandler`, `DeleteMapStyleHandler`, `DeleteGameHandler`) and from `MapProcessor`.
- Coordinate concurrent mutations to the same `Map` aggregate (`AddMapStyleHandler`, `DeleteMapStyleHandler`, `UpdateMapHandler`, `PublishMapHandler`, and `MapProcessor.PublishAsync`) using PostgreSQL row-level locking (`SELECT ... FOR UPDATE`) within their existing database transactions, while keeping `MapStyle` concurrency tokens (`AttemptId`, `Status`) and unique constraints intact.
- Preserve the guarantee that `IMapNotifications` events and work-queue dispatches occur only after the database transaction has committed.
- Out of scope: changes to HTTP API contracts, SignalR hub contracts, map tile processing logic, or database schema migrations.

## Capabilities

### New Capabilities

### Modified Capabilities
- `maps`: Replace process-wide in-memory map mutation lock coordination with PostgreSQL transactional row-level locking on the target map while keeping explicit post-commit SignalR notifications.

## Impact

- `src/GameAtlas.Application/Maps/Processing/IMapMutationLock.cs` (removed)
- `src/GameAtlas.Infrastructure/Maps/Persistence/MapMutationLock.cs` (removed)
- `src/GameAtlas.Infrastructure/DependencyInjection.cs`
- `src/GameAtlas.Application/Abstractions/Persistence/IApplicationDbContext.cs` and `src/GameAtlas.Infrastructure/Persistence/ApplicationDbContext.cs` (or map locking query helper)
- Map and game handlers in `src/GameAtlas.Application/Maps/**` and `src/GameAtlas.Application/Games/DeleteGame/DeleteGameHandler.cs`
- `src/GameAtlas.Infrastructure/Maps/Processing/MapProcessor.cs`
- `tests/GameAtlas.IntegrationTests/MapDispatchTests.cs`
- `src/GameAtlas.Api/Maps/README.md`, `src/GameAtlas.Infrastructure/Maps/README.md`, and `openspec/specs/maps/spec.md`
