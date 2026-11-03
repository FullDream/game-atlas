# SQLite migrations

Run from the repository root with the .NET 10 SDK installed:

```powershell
dotnet tool restore
$env:ConnectionStrings__GameAtlas = 'Data Source=D:/data/game-atlas.db'
dotnet ef migrations add <MigrationName> --project src/GameAtlas.Infrastructure --startup-project src/GameAtlas.Api --output-dir Persistence/Migrations
dotnet ef database update --project src/GameAtlas.Infrastructure --startup-project src/GameAtlas.Api
```

Choose an existing writable parent directory for the SQLite file. The environment variable overrides appsettings configuration; it is not necessary to edit tracked settings. Review and commit generated migrations and the snapshot with the model change. API startup never creates the schema or applies migrations. Apply migrations explicitly before serving requests.

To inspect SQL without changing a database:

```powershell
dotnet ef migrations script --project src/GameAtlas.Infrastructure --startup-project src/GameAtlas.Api
```

To roll back, stop API writes, back up any valuable database file, and target the previous migration:

```powershell
dotnet ef database update <PreviousMigration> --project src/GameAtlas.Infrastructure --startup-project src/GameAtlas.Api
```

For the initial migration, the previous target is `0`. This drops the Games table and deletes its data. Never delete or recreate an existing database to fix migration errors without checking its contents and obtaining authorization for any data loss. Existing databases created without migrations may need an explicit reconciliation plan.

PostgreSQL is not configured yet. SQLite migrations must not be assumed to work unchanged with a future provider.

## Map-owned content transition

`20261024111039_MapOwnedContent` permits several named maps per game and moves category-group ownership
from GameId to MapId. Categories and markers keep their existing parent links. Each map has immutable width/height
and may have no styles. The unique map name key is `(GameId, NormalizedName)`; group name/order indexes now use MapId.
Deleting a map cascades through its current groups, categories, markers, media rows, and styles.
Historical migration descriptions below describe their original schemas, before this ownership transition.

Apply the full migration chain to a fresh empty database. Legacy ownership preparation commands and mapping models have been removed; populated legacy upgrades are unsupported. Historical migration guards remain unchanged and refuse unresolved ownership rather than silently discarding data.

Populated rollback requires matching database/filesystem backups with the preceding application. Empty database upgrades and rollback are tested using disposable stores. Database resets and migration application are always explicit operations.

## Category groups

`CategoryGroups` adds the CategoryGroups table with a required Games foreign key and database cascade deletion, a unique `(GameId, NormalizedName)` index, and an ordered-read `(GameId, Order)` index. Existing Games data is preserved and no backfill is required. Name keys use invariant uppercase normalization computed by the domain, including Latin and Cyrillic casing; public responses keep the trimmed display spelling.

Apply with the `dotnet ef database update` command above before running the updated API. To roll back this migration, target `20261022195257_OrderableGameTimestamps`. Rolling back drops all category groups but preserves Games; back up valuable data and stop writes first. Deleting a game now also permanently deletes its groups, including through the existing direct SQL deletion path.

## Categories

`20261023114301_Categories` adds Categories with a required cascading CategoryGroups foreign key,
a unique `(CategoryGroupId, NormalizedName)` index, and a nonunique `(CategoryGroupId, Order)` index.
Existing games and groups remain unchanged; no backfill is needed. Marker counts are not stored.
Category ownership follows its current group, including after a transfer. Deleting a group or game
physically deletes its current categories.

Apply explicitly before running the updated API:

```powershell
dotnet ef database update 20261023114301_Categories --project src/GameAtlas.Infrastructure --startup-project src/GameAtlas.Api
```

To roll back, stop writes, back up valuable data, deploy the preceding application version, and run:

```powershell
dotnet ef database update 20261023100000_CategoryGroups --project src/GameAtlas.Infrastructure --startup-project src/GameAtlas.Api
```

Rollback drops Categories and all category data while preserving existing games and groups.
Do not apply either command to a persistent database merely to validate the migration; use disposable test databases.

## Markers

`20261023145510_Markers` adds Markers with required category ownership, inline PositionX/PositionY
columns, and the MarkerMedia table with string TPH discriminators and ordered internal child identities.
Deleting a category, group, or game cascades to its current markers and media rows. Referenced files
are never removed. Existing parent data is preserved; category marker counts are computed, not stored.

```powershell
dotnet ef database update 20261023145510_Markers --project src/GameAtlas.Infrastructure --startup-project src/GameAtlas.Api
```

To roll back, stop writes, back up valuable data, deploy the preceding application version, and run:

```powershell
dotnet ef database update 20261023114301_Categories --project src/GameAtlas.Infrastructure --startup-project src/GameAtlas.Api
```

Rollback deletes all marker and media rows and drops their tables, preserving games, groups, and
categories. Validate both directions only against disposable databases.

## Timestamp ordering

`OrderableGameTimestamps` records explicit UTC string conversions for CreatedAt and UpdatedAt so SQLite can sort them before pagination. It changes model metadata only: both migration directions are empty and existing TEXT values remain untouched. The invariant format is `yyyy-MM-dd HH:mm:ss.FFFFFFFzzz`, matching the original SQLite binding, including fractional-second precision. Domain and API values remain DateTimeOffset.

Apply with the same `dotnet ef database update` command above. To roll back only this metadata migration, target `20261022182530_InitialGames`; its Down does not remove game data. Do not target `0` unless intentionally deleting the Games table. Reconsider this provider-specific conversion when adding PostgreSQL.

## Media library schema

Legacy category icons, marker media and map originals are not migrated into the library. No media preparation command, import manifest or legacy-data converter is provided. Initialize a new empty database by explicitly applying the full EF migration chain; upload files through the library API afterwards.

Historical migrations remain unchanged. Existing consumer migration guards refuse unresolved legacy references rather than silently deleting them. A populated legacy database is not a supported upgrade source for this change. Database recreation or deletion of existing files must be requested explicitly; application startup never resets a database or applies migrations.

Keep database and filesystem backups together when required. Populated rollback is unsupported; restore matching backups with the prior application. Fresh empty upgrade and rollback are verified using disposable test stores.

## Category tree

20261101172032_CategoryTree makes Categories.IconAssetId nullable and adds a restrictive optional image FK/index to CategoryGroups. Existing order is normalized by order/createdAt/id within each map/group, preserving visible sequence, IDs, marker/media references and timestamps. Apply explicitly with the EF database update command above; startup does not migrate.

Downgrade rejects null category icons before altering schema. Restore matching backups or explicitly attach valid icons before downgrade; do not invent references. Dropping the group icon column loses those associations, so preserve a backup. Folder/namespace consolidation does not rename tables or marker foreign keys.

## Category group colors

CategoryGroupColor adds required CategoryGroups.Color with default #F59E0B for existing groups, preserving other columns and timestamps. Apply explicitly after CategoryTree through EF CLI. Downgrade removes group color values only.
