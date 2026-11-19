# Tasks

## 1. Per-Map Transactional Row Locking

- [x] 1.1 Add a map row-locking method (using PostgreSQL `SELECT ... FOR UPDATE` on `"Maps"`) to `IApplicationDbContext` and implement it in `ApplicationDbContext`; verify the solution compiles with `dotnet build GameAtlas.slnx`.

## 2. Remove `IMapMutationLock` from Handlers and Processor

- [x] 2.1 Remove `IMapMutationLock` from `AddMapStyleHandler`, `DeleteMapStyleHandler`, `UpdateMapHandler`, `UpdateMapStyleHandler`, and `PublishMapHandler`, replacing the in-memory lock lease with the transactional map row lock before loading/mutating the map aggregate, and ensuring notifications are sent after `transaction.CommitAsync`; verify compilation with `dotnet build GameAtlas.slnx`.
- [x] 2.2 Remove `IMapMutationLock` from `CreateMapHandler`, `DeleteMapHandler`, `RetryMapStyleHandler`, `DeleteGameHandler`, and `MapProcessor` (`RecoverAsync`, `ClaimAsync`, `PublishAsync`, `FailAsync`), acquiring the transactional map row lock in `MapProcessor.PublishAsync` before reconciling the default style and keeping notifications after transaction commit; verify compilation with `dotnet build GameAtlas.slnx`.
- [x] 2.3 Delete `src/GameAtlas.Application/Maps/Processing/IMapMutationLock.cs` and `src/GameAtlas.Infrastructure/Maps/Persistence/MapMutationLock.cs`, and remove the `IMapMutationLock` registration from `src/GameAtlas.Infrastructure/DependencyInjection.cs`; verify `dotnet build src/GameAtlas.Api/GameAtlas.Api.csproj` succeeds with zero references to `IMapMutationLock`.

## 3. Update Integration Tests and Documentation

- [x] 3.1 Update `tests/GameAtlas.IntegrationTests/MapDispatchTests.cs` to remove `IMapMutationLock` usage, verify that `StyleUpdatedAsync` callbacks observe already-committed database changes from an independent scope, and verify `dotnet test tests/GameAtlas.IntegrationTests --filter "FullyQualifiedName~Map"` passes.
- [x] 3.2 Update `src/GameAtlas.Api/Maps/README.md` and `src/GameAtlas.Infrastructure/Maps/README.md` to reflect PostgreSQL transactional row locking instead of `IMapMutationLock`, and verify documentation links with `node scripts/check-docs.mjs` and `git diff --check`.
