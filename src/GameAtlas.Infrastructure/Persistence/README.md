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
