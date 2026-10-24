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

A fresh database with no maps or groups upgrades directly. A populated database requires an explicit preparation:

1. Stop the API, workers, and every other writer. Create a consistent SQLite backup (including any committed WAL
   contents using SQLite's backup facility) and a matching complete map-storage backup. Verify the restore path.
2. Bring the database to `20261023210838_SimplifyMapStyles` using the matching preceding application version.
   Its earlier guard still refuses populated version-based map data; that needs its own agreed transfer first.
3. Inventory every existing map and group. Supply every map's existing ID, game ID, exact dimensions, and an explicit
   display name. For groups whose game has no map, explicitly choose a new UUID v7 map ID, name, width, and height.
   Assign every group exactly once to a map of its existing game. Nothing is guessed from marker positions or names.
4. Prepare an operator-reviewed JSON file, for example:

```json
{
  "maps": [
    {
      "id": "019a0000-0000-7000-8000-000000000004",
      "gameId": "019a0000-0000-7000-8000-000000000001",
      "name": "World",
      "width": 1025,
      "height": 513
    }
  ],
  "groups": [
    {
      "id": "019a0000-0000-7000-8000-000000000002",
      "mapId": "019a0000-0000-7000-8000-000000000004"
    }
  ]
}
```

5. Run the maintenance command with explicit database and mapping paths. It opens an existing database only,
   validates the complete assignment, and atomically writes internal preparation tables. It starts no web server
   or worker and does not apply migrations or move files. Invalid input returns a nonzero exit code without
   changing application data or previously prepared assignments.

```powershell
dotnet run --project src/GameAtlas.Api -c Release -- prepare-map-ownership D:/data/game-atlas.db D:/data/map-ownership.json
$env:ConnectionStrings__GameAtlas = 'Data Source=D:/data/game-atlas.db'
dotnet ef database update --project src/GameAtlas.Infrastructure --startup-project src/GameAtlas.Api
```

6. Check migration history, `PRAGMA foreign_key_check`, map/group destinations, row counts, IDs, timestamps, marker
   positions/media, and existing original/tile bytes before resuming the API. Existing map/style IDs and storage
   paths are preserved. Prepared new maps have no styles; existing content is not rescaled or relocated.

The migration has a `MapOwnership_RequiresExplicitPreparation` guard before ownership changes. Missing map names,
unresolved groups, wrong-game destinations, changed existing dimensions, and conflicting names refuse the transition.
Do not bypass the guard or delete affected data. Correct the mapping while writers remain stopped and rerun preparation.

Nonempty-map rollback is intentionally refused by `MapOwnership_RestoreBackupForNonemptyMaps`: a one-map/game
schema cannot represent the new ownership without losing data. Restore the matching database/storage backups and
preceding application version instead. Empty-map databases can use EF's normal rollback. Validate migrations against
disposable copies; generating or testing the migration does not authorize applying it to persistent data.

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
