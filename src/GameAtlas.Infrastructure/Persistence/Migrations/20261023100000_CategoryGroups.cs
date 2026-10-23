using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameAtlas.Infrastructure.Persistence.Migrations
{
	/// <inheritdoc />
	public partial class CategoryGroups : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.CreateTable(
				name: "CategoryGroups",
				columns: table => new
				{
					Id = table.Column<Guid>(type: "TEXT", nullable: false),
					GameId = table.Column<Guid>(type: "TEXT", nullable: false),
					Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
					NormalizedName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
					Order = table.Column<int>(type: "INTEGER", nullable: false),
					CreatedAt = table.Column<string>(type: "TEXT", nullable: false),
					UpdatedAt = table.Column<string>(type: "TEXT", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_CategoryGroups", x => x.Id);
					table.ForeignKey(
						name: "FK_CategoryGroups_Games_GameId",
						column: x => x.GameId,
						principalTable: "Games",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateIndex(
				name: "IX_CategoryGroups_GameId_NormalizedName",
				table: "CategoryGroups",
				columns: new[] { "GameId", "NormalizedName" },
				unique: true);

			migrationBuilder.CreateIndex(
				name: "IX_CategoryGroups_GameId_Order",
				table: "CategoryGroups",
				columns: new[] { "GameId", "Order" });
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropTable(
				name: "CategoryGroups");
		}
	}
}
