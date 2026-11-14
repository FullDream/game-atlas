using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameAtlas.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class _20261114002958_ContentPublicationStatus : Migration
{
	/// <inheritdoc />
	protected override void Up(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.AddColumn<string>(
			name: "Status",
			table: "Markers",
			type: "TEXT",
			maxLength: 32,
			nullable: false,
			defaultValue: "Published");

		migrationBuilder.AddColumn<string>(
			name: "Status",
			table: "Maps",
			type: "TEXT",
			maxLength: 32,
			nullable: false,
			defaultValue: "Published");

		migrationBuilder.AddColumn<string>(
			name: "Status",
			table: "CategoryGroups",
			type: "TEXT",
			maxLength: 32,
			nullable: false,
			defaultValue: "Published");

		migrationBuilder.AddColumn<string>(
			name: "Status",
			table: "Categories",
			type: "TEXT",
			maxLength: 32,
			nullable: false,
			defaultValue: "Published");
	}

	/// <inheritdoc />
	protected override void Down(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.DropColumn(
			name: "Status",
			table: "Markers");

		migrationBuilder.DropColumn(
			name: "Status",
			table: "Maps");

		migrationBuilder.DropColumn(
			name: "Status",
			table: "CategoryGroups");

		migrationBuilder.DropColumn(
			name: "Status",
			table: "Categories");
	}
}
