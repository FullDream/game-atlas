using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameAtlas.Infrastructure.Persistence.Migrations
{
	/// <inheritdoc />
	public partial class Maps : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropColumn(
				name: "MapId",
				table: "Games");

			migrationBuilder.CreateTable(
				name: "MapAdmissionSessions",
				columns: table => new
				{
					Id = table.Column<Guid>(type: "TEXT", nullable: false),
					ExpiresAt = table.Column<string>(type: "TEXT", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_MapAdmissionSessions", x => x.Id);
				});

			migrationBuilder.CreateTable(
				name: "MapCleanupIntents",
				columns: table => new
				{
					Id = table.Column<Guid>(type: "TEXT", nullable: false),
					SessionId = table.Column<Guid>(type: "TEXT", nullable: true),
					MapId = table.Column<Guid>(type: "TEXT", nullable: true),
					VariantId = table.Column<Guid>(type: "TEXT", nullable: true),
					VersionId = table.Column<Guid>(type: "TEXT", nullable: true),
					OutputsOnly = table.Column<bool>(type: "INTEGER", nullable: false),
					Attempts = table.Column<int>(type: "INTEGER", nullable: false),
					RetryAt = table.Column<string>(type: "TEXT", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_MapCleanupIntents", x => x.Id);
				});

			migrationBuilder.CreateTable(
				name: "Maps",
				columns: table => new
				{
					Id = table.Column<Guid>(type: "TEXT", nullable: false),
					GameId = table.Column<Guid>(type: "TEXT", nullable: false),
					Width = table.Column<int>(type: "INTEGER", nullable: false),
					Height = table.Column<int>(type: "INTEGER", nullable: false),
					LastCreationSequence = table.Column<long>(type: "INTEGER", nullable: false),
					CreatedAt = table.Column<string>(type: "TEXT", nullable: false),
					UpdatedAt = table.Column<string>(type: "TEXT", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Maps", x => x.Id);
					table.ForeignKey(
						name: "FK_Maps_Games_GameId",
						column: x => x.GameId,
						principalTable: "Games",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateTable(
				name: "MapAdmissionSources",
				columns: table => new
				{
					Id = table.Column<Guid>(type: "TEXT", nullable: false),
					SessionId = table.Column<Guid>(type: "TEXT", nullable: false),
					MapId = table.Column<Guid>(type: "TEXT", nullable: false),
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
					MapId = table.Column<Guid>(type: "TEXT", nullable: false),
					Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
					NormalizedName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
					BackgroundColor = table.Column<string>(type: "TEXT", maxLength: 7, nullable: false),
					CreationSequence = table.Column<long>(type: "INTEGER", nullable: false),
					SourceVersionId = table.Column<Guid>(type: "TEXT", nullable: false),
					ActiveVersionId = table.Column<Guid>(type: "TEXT", nullable: true),
					CreatedAt = table.Column<string>(type: "TEXT", nullable: false),
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
					MapVariantId = table.Column<Guid>(type: "TEXT", nullable: false),
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
					table.PrimaryKey("PK_MapSourceVersions", x => x.Id);
					table.ForeignKey(
						name: "FK_MapSourceVersions_MapVariants_MapVariantId",
						column: x => x.MapVariantId,
						principalTable: "MapVariants",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateIndex(
				name: "IX_MapAdmissionSessions_ExpiresAt",
				table: "MapAdmissionSessions",
				column: "ExpiresAt");

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
				name: "IX_MapCleanupIntents_RetryAt",
				table: "MapCleanupIntents",
				column: "RetryAt");

			migrationBuilder.CreateIndex(
				name: "IX_Maps_GameId",
				table: "Maps",
				column: "GameId",
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

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropTable(
				name: "MapAdmissionSources");

			migrationBuilder.DropTable(
				name: "MapCleanupIntents");

			migrationBuilder.DropTable(
				name: "MapSourceVersions");

			migrationBuilder.DropTable(
				name: "MapAdmissionSessions");

			migrationBuilder.DropTable(
				name: "MapVariants");

			migrationBuilder.DropTable(
				name: "Maps");

			migrationBuilder.AddColumn<Guid>(
				name: "MapId",
				table: "Games",
				type: "TEXT",
				nullable: true);
		}
	}
}
