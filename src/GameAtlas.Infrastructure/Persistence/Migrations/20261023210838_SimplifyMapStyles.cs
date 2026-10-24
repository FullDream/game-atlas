using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameAtlas.Infrastructure.Persistence.Migrations
{
	/// <inheritdoc />
	public partial class SimplifyMapStyles : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			// This schema-only upgrade must never discard an existing upload or cleanup owner.
			migrationBuilder.Sql("""
                CREATE TEMP TABLE "__MapStyleUpgradeGuard" (
                    "ExistingRows" INTEGER NOT NULL,
                    CONSTRAINT "MapStyleUpgrade_RequiresEmptyLegacyMapData"
                        CHECK ("ExistingRows" = 0)
                );
                INSERT INTO "__MapStyleUpgradeGuard"
                SELECT
                    (SELECT COUNT(*) FROM "Maps") +
                    (SELECT COUNT(*) FROM "MapVariants") +
                    (SELECT COUNT(*) FROM "MapSourceVersions") +
                    (SELECT COUNT(*) FROM "MapAdmissionSessions") +
                    (SELECT COUNT(*) FROM "MapAdmissionSources") +
                    (SELECT COUNT(*) FROM "MapCleanupIntents");
                DROP TABLE "__MapStyleUpgradeGuard";
                """);

			migrationBuilder.DropTable(
				name: "MapAdmissionSources");

			migrationBuilder.DropTable(
				name: "MapSourceVersions");

			migrationBuilder.DropTable(
				name: "MapVariants");

			migrationBuilder.DropColumn(
				name: "VariantId",
				table: "MapCleanupIntents");

			migrationBuilder.RenameColumn(
				name: "VersionId",
				table: "MapCleanupIntents",
				newName: "StyleId");

			migrationBuilder.CreateTable(
				name: "MapAdmissionStyles",
				columns: table => new
				{
					Id = table.Column<Guid>(type: "TEXT", nullable: false),
					SessionId = table.Column<Guid>(type: "TEXT", nullable: false),
					MapId = table.Column<Guid>(type: "TEXT", nullable: false),
					StyleId = table.Column<Guid>(type: "TEXT", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_MapAdmissionStyles", x => x.Id);
					table.ForeignKey(
						name: "FK_MapAdmissionStyles_MapAdmissionSessions_SessionId",
						column: x => x.SessionId,
						principalTable: "MapAdmissionSessions",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateTable(
				name: "MapStyles",
				columns: table => new
				{
					Id = table.Column<Guid>(type: "TEXT", nullable: false),
					MapId = table.Column<Guid>(type: "TEXT", nullable: false),
					Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
					NormalizedName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
					BackgroundColor = table.Column<string>(type: "TEXT", maxLength: 7, nullable: false),
					CreationSequence = table.Column<long>(type: "INTEGER", nullable: false),
					Format = table.Column<string>(type: "TEXT", maxLength: 4, nullable: false),
					Width = table.Column<int>(type: "INTEGER", nullable: false),
					Height = table.Column<int>(type: "INTEGER", nullable: false),
					ByteLength = table.Column<long>(type: "INTEGER", nullable: false),
					Status = table.Column<string>(type: "TEXT", nullable: false),
					Progress = table.Column<int>(type: "INTEGER", nullable: false),
					ErrorCode = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
					ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
					AttemptId = table.Column<Guid>(type: "TEXT", nullable: false),
					CreatedAt = table.Column<string>(type: "TEXT", nullable: false),
					UpdatedAt = table.Column<string>(type: "TEXT", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_MapStyles", x => x.Id);
					table.ForeignKey(
						name: "FK_MapStyles_Maps_MapId",
						column: x => x.MapId,
						principalTable: "Maps",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateIndex(
				name: "IX_MapAdmissionStyles_SessionId",
				table: "MapAdmissionStyles",
				column: "SessionId");

			migrationBuilder.CreateIndex(
				name: "IX_MapAdmissionStyles_StyleId",
				table: "MapAdmissionStyles",
				column: "StyleId",
				unique: true);

			migrationBuilder.CreateIndex(
				name: "IX_MapStyles_MapId_CreationSequence",
				table: "MapStyles",
				columns: new[] { "MapId", "CreationSequence" },
				unique: true);

			migrationBuilder.CreateIndex(
				name: "IX_MapStyles_MapId_NormalizedName",
				table: "MapStyles",
				columns: new[] { "MapId", "NormalizedName" },
				unique: true);

			migrationBuilder.CreateIndex(
				name: "IX_MapStyles_Status_CreatedAt",
				table: "MapStyles",
				columns: new[] { "Status", "CreatedAt" });
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropTable(
				name: "MapAdmissionStyles");

			migrationBuilder.DropTable(
				name: "MapStyles");

			migrationBuilder.RenameColumn(
				name: "StyleId",
				table: "MapCleanupIntents",
				newName: "VersionId");

			migrationBuilder.AddColumn<Guid>(
				name: "VariantId",
				table: "MapCleanupIntents",
				type: "TEXT",
				nullable: true);

			migrationBuilder.CreateTable(
				name: "MapAdmissionSources",
				columns: table => new
				{
					Id = table.Column<Guid>(type: "TEXT", nullable: false),
					MapId = table.Column<Guid>(type: "TEXT", nullable: false),
					SessionId = table.Column<Guid>(type: "TEXT", nullable: false),
					VariantId = table.Column<Guid>(type: "TEXT", nullable: false),
					VersionId = table.Column<Guid>(type: "TEXT", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_MapAdmissionSources", x => x.Id);
					table.ForeignKey(
						name: "FK_MapAdmissionSources_MapAdmissionSessions_SessionId",
						column: x => x.SessionId,
						principalTable: "MapAdmissionSessions",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateTable(
				name: "MapVariants",
				columns: table => new
				{
					Id = table.Column<Guid>(type: "TEXT", nullable: false),
					ActiveVersionId = table.Column<Guid>(type: "TEXT", nullable: true),
					BackgroundColor = table.Column<string>(type: "TEXT", maxLength: 7, nullable: false),
					CreatedAt = table.Column<string>(type: "TEXT", nullable: false),
					CreationSequence = table.Column<long>(type: "INTEGER", nullable: false),
					MapId = table.Column<Guid>(type: "TEXT", nullable: false),
					Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
					NormalizedName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
					SourceVersionId = table.Column<Guid>(type: "TEXT", nullable: false),
					UpdatedAt = table.Column<string>(type: "TEXT", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_MapVariants", x => x.Id);
					table.ForeignKey(
						name: "FK_MapVariants_Maps_MapId",
						column: x => x.MapId,
						principalTable: "Maps",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateTable(
				name: "MapSourceVersions",
				columns: table => new
				{
					Id = table.Column<Guid>(type: "TEXT", nullable: false),
					AttemptId = table.Column<Guid>(type: "TEXT", nullable: false),
					ByteLength = table.Column<long>(type: "INTEGER", nullable: false),
					CreatedAt = table.Column<string>(type: "TEXT", nullable: false),
					ErrorCode = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
					ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
					Format = table.Column<string>(type: "TEXT", maxLength: 4, nullable: false),
					Height = table.Column<int>(type: "INTEGER", nullable: false),
					MapVariantId = table.Column<Guid>(type: "TEXT", nullable: false),
					Progress = table.Column<int>(type: "INTEGER", nullable: false),
					Status = table.Column<string>(type: "TEXT", nullable: false),
					UpdatedAt = table.Column<string>(type: "TEXT", nullable: false),
					Width = table.Column<int>(type: "INTEGER", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_MapSourceVersions", x => x.Id);
					table.ForeignKey(
						name: "FK_MapSourceVersions_MapVariants_MapVariantId",
						column: x => x.MapVariantId,
						principalTable: "MapVariants",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateIndex(
				name: "IX_MapAdmissionSources_SessionId",
				table: "MapAdmissionSources",
				column: "SessionId");

			migrationBuilder.CreateIndex(
				name: "IX_MapAdmissionSources_VersionId",
				table: "MapAdmissionSources",
				column: "VersionId",
				unique: true);

			migrationBuilder.CreateIndex(
				name: "IX_MapSourceVersions_MapVariantId",
				table: "MapSourceVersions",
				column: "MapVariantId");

			migrationBuilder.CreateIndex(
				name: "IX_MapSourceVersions_Status_CreatedAt",
				table: "MapSourceVersions",
				columns: new[] { "Status", "CreatedAt" });

			migrationBuilder.CreateIndex(
				name: "IX_MapVariants_MapId_CreationSequence",
				table: "MapVariants",
				columns: new[] { "MapId", "CreationSequence" },
				unique: true);

			migrationBuilder.CreateIndex(
				name: "IX_MapVariants_MapId_NormalizedName",
				table: "MapVariants",
				columns: new[] { "MapId", "NormalizedName" },
				unique: true);
		}
	}
}
