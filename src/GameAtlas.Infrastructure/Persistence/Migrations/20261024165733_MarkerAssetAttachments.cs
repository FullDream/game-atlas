using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameAtlas.Infrastructure.Persistence.Migrations
{
	/// <inheritdoc />
	public partial class MarkerAssetAttachments : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.Sql("""
				CREATE TEMP TABLE "__MarkerAssetGuard" (
					"Count" INTEGER NOT NULL CONSTRAINT "Media_RequiresEmptyLegacyMarkerMedia"
					CHECK ("Count" = 0)
				);
				INSERT INTO "__MarkerAssetGuard" SELECT COUNT(*) FROM "MarkerMedia";
				DROP TABLE "__MarkerAssetGuard";
				""");

			migrationBuilder.CreateTable(
				name: "MarkerAssets",
				columns: table => new
				{
					MarkerId = table.Column<Guid>(type: "TEXT", nullable: false),
					AssetId = table.Column<Guid>(type: "TEXT", nullable: false),
					Order = table.Column<int>(type: "INTEGER", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_MarkerAssets", x => new { x.MarkerId, x.AssetId });
					table.ForeignKey(
						name: "FK_MarkerAssets_Markers_MarkerId",
						column: x => x.MarkerId,
						principalTable: "Markers",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
					table.ForeignKey(
						name: "FK_MarkerAssets_MediaAssets_AssetId",
						column: x => x.AssetId,
						principalTable: "MediaAssets",
						principalColumn: "Id",
						onDelete: ReferentialAction.Restrict);
				});

			migrationBuilder.CreateIndex(
				name: "IX_MarkerAssets_AssetId",
				table: "MarkerAssets",
				column: "AssetId");

			migrationBuilder.CreateIndex(
				name: "IX_MarkerAssets_MarkerId_Order",
				table: "MarkerAssets",
				columns: new[] { "MarkerId", "Order" });

			migrationBuilder.DropTable(name: "MarkerMedia");
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.Sql("""
				CREATE TEMP TABLE "__MarkerAssetRollbackGuard" (
					"Count" INTEGER NOT NULL CONSTRAINT "Media_RestoreBackupForNonemptyMarkerAssets"
					CHECK ("Count" = 0)
				);
				INSERT INTO "__MarkerAssetRollbackGuard" SELECT COUNT(*) FROM "MarkerAssets";
				DROP TABLE "__MarkerAssetRollbackGuard";
				""");
			migrationBuilder.DropTable(
				name: "MarkerAssets");

			migrationBuilder.CreateTable(
				name: "MarkerMedia",
				columns: table => new
				{
					Id = table.Column<Guid>(type: "TEXT", nullable: false),
					MarkerId = table.Column<Guid>(type: "TEXT", nullable: false),
					Order = table.Column<int>(type: "INTEGER", nullable: false),
					Type = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
					Alt = table.Column<string>(type: "TEXT", nullable: true),
					Height = table.Column<int>(type: "INTEGER", nullable: true),
					Url = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
					Width = table.Column<int>(type: "INTEGER", nullable: true),
					Preview = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
					VideoId = table.Column<string>(type: "TEXT", maxLength: 11, nullable: true)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_MarkerMedia", x => x.Id);
					table.ForeignKey(
						name: "FK_MarkerMedia_Markers_MarkerId",
						column: x => x.MarkerId,
						principalTable: "Markers",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateIndex(
				name: "IX_MarkerMedia_MarkerId_Order",
				table: "MarkerMedia",
				columns: new[] { "MarkerId", "Order" });
		}
	}
}
