using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameAtlas.Infrastructure.Persistence.Migrations
{
	/// <inheritdoc />
	public partial class CategoryTree : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.Sql("""
                WITH ranked AS (SELECT Id, ROW_NUMBER() OVER (PARTITION BY MapId ORDER BY "Order", CreatedAt, Id) AS Position FROM CategoryGroups)
                UPDATE CategoryGroups SET "Order" = (SELECT Position FROM ranked WHERE ranked.Id = CategoryGroups.Id);
                WITH ranked AS (SELECT Id, ROW_NUMBER() OVER (PARTITION BY CategoryGroupId ORDER BY "Order", CreatedAt, Id) AS Position FROM Categories)
                UPDATE Categories SET "Order" = (SELECT Position FROM ranked WHERE ranked.Id = Categories.Id);
                """);
			migrationBuilder.AddColumn<Guid>(
				name: "IconAssetId",
				table: "CategoryGroups",
				type: "TEXT",
				nullable: true);

			migrationBuilder.AlterColumn<Guid>(
				name: "IconAssetId",
				table: "Categories",
				type: "TEXT",
				nullable: true,
				oldClrType: typeof(Guid),
				oldType: "TEXT");

			migrationBuilder.CreateIndex(
				name: "IX_CategoryGroups_IconAssetId",
				table: "CategoryGroups",
				column: "IconAssetId");

			migrationBuilder.AddForeignKey(
				name: "FK_CategoryGroups_MediaAssets_IconAssetId",
				table: "CategoryGroups",
				column: "IconAssetId",
				principalTable: "MediaAssets",
				principalColumn: "Id",
				onDelete: ReferentialAction.Restrict);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.Sql("""
                CREATE TABLE "__CategoryTreeRollbackGuard" (NullIcons INTEGER CONSTRAINT CategoryTree_RequiresCategoryIcons CHECK (NullIcons = 0));
                INSERT INTO "__CategoryTreeRollbackGuard" SELECT COUNT(*) FROM Categories WHERE IconAssetId IS NULL;
                DROP TABLE "__CategoryTreeRollbackGuard";
                """);
			migrationBuilder.DropForeignKey(
				name: "FK_CategoryGroups_MediaAssets_IconAssetId",
				table: "CategoryGroups");

			migrationBuilder.DropIndex(
				name: "IX_CategoryGroups_IconAssetId",
				table: "CategoryGroups");

			migrationBuilder.DropColumn(
				name: "IconAssetId",
				table: "CategoryGroups");

			migrationBuilder.AlterColumn<Guid>(
				name: "IconAssetId",
				table: "Categories",
				type: "TEXT",
				nullable: false,
				oldClrType: typeof(Guid),
				oldType: "TEXT",
				oldNullable: true);
		}
	}
}
