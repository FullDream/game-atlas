using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameAtlas.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class _20261113202720_MapSlugs : Migration
{
	/// <inheritdoc />
	protected override void Up(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.AddColumn<string>(
			name: "Slug",
			table: "Maps",
			type: "TEXT",
			maxLength: 200,
			nullable: true);

		migrationBuilder.Sql("UPDATE Maps SET Slug = 'map-' || lower(replace(Id, '-', ''));");

		migrationBuilder.AlterColumn<string>(
			name: "Slug",
			table: "Maps",
			type: "TEXT",
			maxLength: 200,
			nullable: false,
			oldClrType: typeof(string),
			oldType: "TEXT",
			oldMaxLength: 200,
			oldNullable: true);

		migrationBuilder.CreateIndex(
			name: "IX_Maps_GameId_Slug",
			table: "Maps",
			columns: new[] { "GameId", "Slug" },
			unique: true);
	}

	/// <inheritdoc />
	protected override void Down(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.DropIndex(
			name: "IX_Maps_GameId_Slug",
			table: "Maps");

		migrationBuilder.DropColumn(
			name: "Slug",
			table: "Maps");
	}
}
