using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameAtlas.Infrastructure.Persistence.Migrations
{
	/// <inheritdoc />
	public partial class GameImageAssets : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.Sql("""
				CREATE TEMP TABLE "__GameImageGuard" (
					"Count" INTEGER NOT NULL CONSTRAINT "GameImages_RequiresEmptyLegacyGames" CHECK ("Count" = 0)
				);
				INSERT INTO "__GameImageGuard" SELECT COUNT(*) FROM "Games";
				DROP TABLE "__GameImageGuard";
				""");

			migrationBuilder.DropColumn(
				name: "Image",
				table: "Games");

			migrationBuilder.AddColumn<Guid>(
				name: "ImageAssetId",
				table: "Games",
				type: "TEXT",
				nullable: false,
				defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

			migrationBuilder.CreateIndex(
				name: "IX_Games_ImageAssetId",
				table: "Games",
				column: "ImageAssetId");

			migrationBuilder.AddForeignKey(
				name: "FK_Games_MediaAssets_ImageAssetId",
				table: "Games",
				column: "ImageAssetId",
				principalTable: "MediaAssets",
				principalColumn: "Id",
				onDelete: ReferentialAction.Restrict);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.Sql("""
				CREATE TEMP TABLE "__GameImageRollbackGuard" (
					"Count" INTEGER NOT NULL CONSTRAINT "GameImages_RestoreBackupForNonemptyGames"
					CHECK ("Count" = 0)
				);
				INSERT INTO "__GameImageRollbackGuard" SELECT COUNT(*) FROM "Games";
				DROP TABLE "__GameImageRollbackGuard";
				""");
			migrationBuilder.DropForeignKey(
				name: "FK_Games_MediaAssets_ImageAssetId",
				table: "Games");

			migrationBuilder.DropIndex(
				name: "IX_Games_ImageAssetId",
				table: "Games");

			migrationBuilder.DropColumn(
				name: "ImageAssetId",
				table: "Games");

			migrationBuilder.AddColumn<string>(
				name: "Image",
				table: "Games",
				type: "TEXT",
				maxLength: 2048,
				nullable: false,
				defaultValue: "");
		}
	}
}
