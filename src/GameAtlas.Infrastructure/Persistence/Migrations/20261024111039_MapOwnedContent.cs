using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameAtlas.Infrastructure.Persistence.Migrations
{
	/// <inheritdoc />
	public partial class MapOwnedContent : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS __MapOwnershipMaps (
                    Id TEXT PRIMARY KEY, GameId TEXT NOT NULL, Name TEXT NOT NULL, NormalizedName TEXT NOT NULL,
                    Width INTEGER NOT NULL, Height INTEGER NOT NULL, CreatedAt TEXT NOT NULL);
                CREATE TABLE IF NOT EXISTS __MapOwnershipGroups (Id TEXT PRIMARY KEY, MapId TEXT NOT NULL);
                CREATE TEMP TABLE __MapOwnershipGuard (
                    InvalidRows INTEGER NOT NULL,
                    CONSTRAINT MapOwnership_RequiresExplicitPreparation CHECK (InvalidRows = 0));
                INSERT INTO __MapOwnershipGuard
                SELECT
                    (SELECT COUNT(*) FROM Maps m LEFT JOIN __MapOwnershipMaps p ON p.Id = m.Id
                        WHERE p.Id IS NULL OR p.GameId != m.GameId OR p.Width != m.Width OR p.Height != m.Height)
                    + (SELECT COUNT(*) FROM CategoryGroups g
                        LEFT JOIN __MapOwnershipGroups a ON a.Id = g.Id
                        LEFT JOIN __MapOwnershipMaps p ON p.Id = a.MapId
                        WHERE p.Id IS NULL OR p.GameId != g.GameId)
                    + (SELECT COUNT(*) FROM __MapOwnershipGroups a
                        LEFT JOIN CategoryGroups g ON g.Id = a.Id WHERE g.Id IS NULL)
                    + (SELECT COUNT(*) FROM __MapOwnershipMaps p LEFT JOIN Games g ON g.Id = p.GameId
                        WHERE g.Id IS NULL OR length(trim(p.Name)) NOT BETWEEN 1 AND 200
                            OR p.Name != trim(p.Name) OR length(p.NormalizedName) NOT BETWEEN 1 AND 200
                            OR p.Width NOT BETWEEN 1 AND 16384 OR p.Height NOT BETWEEN 1 AND 16384)
                    + (SELECT COUNT(*) FROM (SELECT GameId, NormalizedName FROM __MapOwnershipMaps
                        GROUP BY GameId, NormalizedName HAVING COUNT(*) > 1));
                DROP TABLE __MapOwnershipGuard;
                """);

			// SQLite rebuilds CategoryGroups; preserve its descendants while remapping the old FK values.
			migrationBuilder.Sql("PRAGMA foreign_keys = 0;", suppressTransaction: true);

			migrationBuilder.DropForeignKey(
				name: "FK_CategoryGroups_Games_GameId",
				table: "CategoryGroups");

			migrationBuilder.DropIndex(
				name: "IX_Maps_GameId",
				table: "Maps");

			migrationBuilder.RenameColumn(
				name: "GameId",
				table: "CategoryGroups",
				newName: "MapId");

			migrationBuilder.RenameIndex(
				name: "IX_CategoryGroups_GameId_Order",
				table: "CategoryGroups",
				newName: "IX_CategoryGroups_MapId_Order");

			migrationBuilder.RenameIndex(
				name: "IX_CategoryGroups_GameId_NormalizedName",
				table: "CategoryGroups",
				newName: "IX_CategoryGroups_MapId_NormalizedName");

			migrationBuilder.AddColumn<string>(
				name: "Name",
				table: "Maps",
				type: "TEXT",
				maxLength: 200,
				nullable: false,
				defaultValue: "");

			migrationBuilder.AddColumn<string>(
				name: "NormalizedName",
				table: "Maps",
				type: "TEXT",
				maxLength: 200,
				nullable: false,
				defaultValue: "");

			migrationBuilder.Sql("""
                UPDATE Maps SET
                    Name = (SELECT Name FROM __MapOwnershipMaps p WHERE p.Id = Maps.Id),
                    NormalizedName = (SELECT NormalizedName FROM __MapOwnershipMaps p WHERE p.Id = Maps.Id);
                INSERT INTO Maps (Id, GameId, Name, NormalizedName, Width, Height,
                    LastCreationSequence, CreatedAt, UpdatedAt)
                SELECT Id, GameId, Name, NormalizedName, Width, Height, 0, CreatedAt, CreatedAt
                FROM __MapOwnershipMaps p WHERE NOT EXISTS (SELECT 1 FROM Maps m WHERE m.Id = p.Id);
                UPDATE CategoryGroups SET MapId =
                    (SELECT MapId FROM __MapOwnershipGroups a WHERE a.Id = CategoryGroups.Id);
                DROP TABLE __MapOwnershipGroups;
                DROP TABLE __MapOwnershipMaps;
                """);

			migrationBuilder.CreateIndex(
				name: "IX_Maps_GameId",
				table: "Maps",
				column: "GameId");

			migrationBuilder.CreateIndex(
				name: "IX_Maps_GameId_NormalizedName",
				table: "Maps",
				columns: new[] { "GameId", "NormalizedName" },
				unique: true);

			migrationBuilder.AddForeignKey(
				name: "FK_CategoryGroups_Maps_MapId",
				table: "CategoryGroups",
				column: "MapId",
				principalTable: "Maps",
				principalColumn: "Id",
				onDelete: ReferentialAction.Cascade);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.Sql("""
                CREATE TEMP TABLE __MapOwnershipRollbackGuard (
                    ExistingMaps INTEGER NOT NULL,
                    CONSTRAINT MapOwnership_RestoreBackupForNonemptyMaps CHECK (ExistingMaps = 0));
                INSERT INTO __MapOwnershipRollbackGuard SELECT COUNT(*) FROM Maps;
                DROP TABLE __MapOwnershipRollbackGuard;
                """);

			migrationBuilder.DropForeignKey(
				name: "FK_CategoryGroups_Maps_MapId",
				table: "CategoryGroups");

			migrationBuilder.DropIndex(
				name: "IX_Maps_GameId",
				table: "Maps");

			migrationBuilder.DropIndex(
				name: "IX_Maps_GameId_NormalizedName",
				table: "Maps");

			migrationBuilder.DropColumn(
				name: "Name",
				table: "Maps");

			migrationBuilder.DropColumn(
				name: "NormalizedName",
				table: "Maps");

			migrationBuilder.RenameColumn(
				name: "MapId",
				table: "CategoryGroups",
				newName: "GameId");

			migrationBuilder.RenameIndex(
				name: "IX_CategoryGroups_MapId_Order",
				table: "CategoryGroups",
				newName: "IX_CategoryGroups_GameId_Order");

			migrationBuilder.RenameIndex(
				name: "IX_CategoryGroups_MapId_NormalizedName",
				table: "CategoryGroups",
				newName: "IX_CategoryGroups_GameId_NormalizedName");

			migrationBuilder.CreateIndex(
				name: "IX_Maps_GameId",
				table: "Maps",
				column: "GameId",
				unique: true);

			migrationBuilder.AddForeignKey(
				name: "FK_CategoryGroups_Games_GameId",
				table: "CategoryGroups",
				column: "GameId",
				principalTable: "Games",
				principalColumn: "Id",
				onDelete: ReferentialAction.Cascade);
		}
	}
}
