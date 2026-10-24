using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameAtlas.Infrastructure.Persistence.Migrations
{
	/// <inheritdoc />
	public partial class CategoryIconAssets : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.Sql("""
				CREATE TEMP TABLE "__CategoryIconGuard" (
					"Count" INTEGER NOT NULL CONSTRAINT "Media_RequiresEmptyLegacyCategories"
					CHECK ("Count" = 0)
				);
				INSERT INTO "__CategoryIconGuard" SELECT COUNT(*) FROM "Categories";
				DROP TABLE "__CategoryIconGuard";
				""");

			migrationBuilder.RenameColumn(name: "Icon", table: "Categories", newName: "IconAssetId");

			migrationBuilder.CreateIndex(
				name: "IX_Categories_IconAssetId",
				table: "Categories",
				column: "IconAssetId");

			migrationBuilder.AddForeignKey(
				name: "FK_Categories_MediaAssets_IconAssetId",
				table: "Categories",
				column: "IconAssetId",
				principalTable: "MediaAssets",
				principalColumn: "Id",
				onDelete: ReferentialAction.Restrict);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.Sql("""
				CREATE TEMP TABLE "__CategoryIconRollbackGuard" (
					"Count" INTEGER NOT NULL CONSTRAINT "Media_RestoreBackupForNonemptyCategories"
					CHECK ("Count" = 0)
				);
				INSERT INTO "__CategoryIconRollbackGuard" SELECT COUNT(*) FROM "Categories";
				DROP TABLE "__CategoryIconRollbackGuard";
				""");

			migrationBuilder.DropForeignKey(
				name: "FK_Categories_MediaAssets_IconAssetId",
				table: "Categories");

			migrationBuilder.DropIndex(
				name: "IX_Categories_IconAssetId",
				table: "Categories");

			migrationBuilder.DropColumn(
				name: "IconAssetId",
				table: "Categories");

			migrationBuilder.AddColumn<string>(
				name: "Icon",
				table: "Categories",
				type: "TEXT",
				maxLength: 2048,
				nullable: false,
				defaultValue: "");
		}
	}
}
