using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameAtlas.Infrastructure.Persistence.Migrations
{
	/// <inheritdoc />
	public partial class SharedMapStyleSources : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.Sql("""
				CREATE TEMP TABLE "__MapSourceGuard" (
					"Count" INTEGER NOT NULL CONSTRAINT "Media_RequiresEmptyLegacyStylesAndAdmissions"
					CHECK ("Count" = 0)
				);
				INSERT INTO "__MapSourceGuard" SELECT
					(SELECT COUNT(*) FROM "MapAdmissionSessions") +
					(SELECT COUNT(*) FROM "MapAdmissionStyles") +
					(SELECT COUNT(*) FROM "MapStyles");
				DROP TABLE "__MapSourceGuard";
				""");

			migrationBuilder.RenameColumn(name: "Format", table: "MapStyles", newName: "SourceAssetId");
			migrationBuilder.DropColumn(name: "Width", table: "MapStyles");
			migrationBuilder.DropColumn(name: "Height", table: "MapStyles");
			migrationBuilder.DropColumn(name: "ByteLength", table: "MapStyles");

			migrationBuilder.CreateIndex(
				name: "IX_MapStyles_SourceAssetId",
				table: "MapStyles",
				column: "SourceAssetId");

			migrationBuilder.AddForeignKey(
				name: "FK_MapStyles_MediaAssets_SourceAssetId",
				table: "MapStyles",
				column: "SourceAssetId",
				principalTable: "MediaAssets",
				principalColumn: "Id",
				onDelete: ReferentialAction.Restrict);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.Sql("""
				CREATE TEMP TABLE "__MapSourceRollbackGuard" (
					"Count" INTEGER NOT NULL CONSTRAINT "Media_RestoreBackupForNonemptyMapStyles"
					CHECK ("Count" = 0)
				);
				INSERT INTO "__MapSourceRollbackGuard" SELECT COUNT(*) FROM "MapStyles";
				DROP TABLE "__MapSourceRollbackGuard";
				""");

			migrationBuilder.DropForeignKey(
				name: "FK_MapStyles_MediaAssets_SourceAssetId",
				table: "MapStyles");

			migrationBuilder.DropIndex(
				name: "IX_MapStyles_SourceAssetId",
				table: "MapStyles");

			migrationBuilder.DropColumn(
				name: "SourceAssetId",
				table: "MapStyles");

			migrationBuilder.AddColumn<long>(
				name: "ByteLength",
				table: "MapStyles",
				type: "INTEGER",
				nullable: false,
				defaultValue: 0L);

			migrationBuilder.AddColumn<string>(
				name: "Format",
				table: "MapStyles",
				type: "TEXT",
				maxLength: 4,
				nullable: false,
				defaultValue: "");

			migrationBuilder.AddColumn<int>(
				name: "Height",
				table: "MapStyles",
				type: "INTEGER",
				nullable: false,
				defaultValue: 0);

			migrationBuilder.AddColumn<int>(
				name: "Width",
				table: "MapStyles",
				type: "INTEGER",
				nullable: false,
				defaultValue: 0);
		}
	}
}
