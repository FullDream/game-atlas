using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameAtlas.Infrastructure.Persistence.Migrations
{
	/// <inheritdoc />
	public partial class ExpandedGameMetadata : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropForeignKey(
				name: "FK_Games_MediaAssets_ImageAssetId",
				table: "Games");

			migrationBuilder.RenameColumn(
				name: "ImageAssetId",
				table: "Games",
				newName: "CoverId");

			migrationBuilder.RenameIndex(
				name: "IX_Games_ImageAssetId",
				table: "Games",
				newName: "IX_Games_CoverId");

			migrationBuilder.AddColumn<Guid>(
				name: "BannerId",
				table: "Games",
				type: "TEXT",
				nullable: true);

			migrationBuilder.AddColumn<string>(
				name: "Description",
				table: "Games",
				type: "TEXT",
				nullable: true);

			migrationBuilder.AddColumn<string>(
				name: "Developer",
				table: "Games",
				type: "TEXT",
				nullable: true);

			migrationBuilder.AddColumn<DateOnly>(
				name: "ReleaseDate",
				table: "Games",
				type: "TEXT",
				nullable: true);

			migrationBuilder.CreateIndex(
				name: "IX_Games_BannerId",
				table: "Games",
				column: "BannerId");

			migrationBuilder.AddForeignKey(
				name: "FK_Games_MediaAssets_BannerId",
				table: "Games",
				column: "BannerId",
				principalTable: "MediaAssets",
				principalColumn: "Id",
				onDelete: ReferentialAction.Restrict);

			migrationBuilder.AddForeignKey(
				name: "FK_Games_MediaAssets_CoverId",
				table: "Games",
				column: "CoverId",
				principalTable: "MediaAssets",
				principalColumn: "Id",
				onDelete: ReferentialAction.Restrict);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropForeignKey(
				name: "FK_Games_MediaAssets_BannerId",
				table: "Games");

			migrationBuilder.DropForeignKey(
				name: "FK_Games_MediaAssets_CoverId",
				table: "Games");

			migrationBuilder.DropIndex(
				name: "IX_Games_BannerId",
				table: "Games");

			migrationBuilder.DropColumn(
				name: "BannerId",
				table: "Games");

			migrationBuilder.DropColumn(
				name: "Description",
				table: "Games");

			migrationBuilder.DropColumn(
				name: "Developer",
				table: "Games");

			migrationBuilder.DropColumn(
				name: "ReleaseDate",
				table: "Games");

			migrationBuilder.RenameColumn(
				name: "CoverId",
				table: "Games",
				newName: "ImageAssetId");

			migrationBuilder.RenameIndex(
				name: "IX_Games_CoverId",
				table: "Games",
				newName: "IX_Games_ImageAssetId");

			migrationBuilder.AddForeignKey(
				name: "FK_Games_MediaAssets_ImageAssetId",
				table: "Games",
				column: "ImageAssetId",
				principalTable: "MediaAssets",
				principalColumn: "Id",
				onDelete: ReferentialAction.Restrict);
		}
	}
}
