using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameAtlas.Infrastructure.Persistence.Migrations
{
	/// <inheritdoc />
	public partial class InitialGames : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.CreateTable(
				name: "Games",
				columns: table => new
				{
					Id = table.Column<Guid>(type: "TEXT", nullable: false),
					Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
					Slug = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
					Image = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
					MapId = table.Column<Guid>(type: "TEXT", nullable: true),
					CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
					UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
					Status = table.Column<string>(type: "TEXT", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Games", x => x.Id);
				});

			migrationBuilder.CreateIndex(
				name: "IX_Games_Slug",
				table: "Games",
				column: "Slug",
				unique: true);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropTable(
				name: "Games");
		}
	}
}
