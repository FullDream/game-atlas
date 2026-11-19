# PostgreSQL migrations

Run from the repository root with the .NET 11 SDK installed:

```powershell
dotnet tool restore
$env:ConnectionStrings__GameAtlas = 'Host=localhost;Port=5432;Database=game_atlas;Username=game_atlas;Password=game_atlas'
dotnet ef migrations add <MigrationName> --project src/GameAtlas.Infrastructure --startup-project src/GameAtlas.Api --output-dir Persistence/Migrations
dotnet ef database update --project src/GameAtlas.Infrastructure --startup-project src/GameAtlas.Api
```

Ensure a reachable PostgreSQL instance exists for the configured connection string. The environment variable overrides appsettings configuration; it is not necessary to edit tracked settings. Review and commit generated migrations and the snapshot with the model change. API startup never creates the schema or applies migrations. Apply migrations explicitly before serving requests.

To inspect SQL without changing a database:

```powershell
dotnet ef migrations script --project src/GameAtlas.Infrastructure --startup-project src/GameAtlas.Api
```

To roll back, stop API writes, back up any valuable database, and target the previous migration:

```powershell
dotnet ef database update <PreviousMigration> --project src/GameAtlas.Infrastructure --startup-project src/GameAtlas.Api
```

For the initial migration (`Initial`), the previous target is `0`. This drops all tables and deletes their data. Never delete or recreate an existing database to fix migration errors without checking its contents and obtaining authorization for any data loss. Existing databases created without migrations may need an explicit reconciliation plan.

## Initial schema

`Initial` creates the complete PostgreSQL schema in a single baseline migration:

- **Identity**: `AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, `AspNetUserClaims`, `AspNetRoleClaims`, `AspNetUserLogins`, `AspNetUserTokens`
- **Media**: `MediaFolders` (with partial unique index `IX_MediaFolders_NormalizedName` where `"ParentId" IS NULL` and composite unique index `IX_MediaFolders_ParentId_NormalizedName`) and TPH `MediaAssets` (`image` / `video` discriminator)
- **Games**: `Games` with unique `Slug` (`IX_Games_Slug`), required `CoverId`, optional `BannerId`, metadata columns, and string `Status`
- **Maps**: `Maps` (unique `(GameId, Slug)` and `(GameId, NormalizedName)`), `MapStyles` (unique `(MapId, NormalizedName)`), and `MapCleanupIntents`
- **Categories & Markers**: `CategoryGroups` (unique `(MapId, NormalizedName)` and ordered `(MapId, Order)`), `Categories` (unique `(CategoryGroupId, NormalizedName)` and ordered `(CategoryGroupId, Order)`), `Markers` (inline `PositionX`/`PositionY`), and `MarkerAssets`
- **Timestamps**: All `DateTimeOffset` properties use native PostgreSQL `timestamp with time zone` (`timestamptz`) columns.
