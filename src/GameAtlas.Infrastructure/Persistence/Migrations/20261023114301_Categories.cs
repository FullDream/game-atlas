using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameAtlas.Infrastructure.Persistence.Migrations
{
	/// <inheritdoc />
	public partial class Categories : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.CreateTable(
				name: "Categories",
				columns: table => new
				{
					Id = table.Column<Guid>(type: "TEXT", nullable: false),
					CategoryGroupId = table.Column<Guid>(type: "TEXT", nullable: false),
					Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
					NormalizedName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
					Icon = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
					Color = table.Column<string>(type: "TEXT", maxLength: 7, nullable: false),
					Order = table.Column<int>(type: "INTEGER", nullable: false),
					CreatedAt = table.Column<string>(type: "TEXT", nullable: false),
					UpdatedAt = table.Column<string>(type: "TEXT", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Categories", x => x.Id);
					table.ForeignKey(
						name: "FK_Categories_CategoryGroups_CategoryGroupId",
						column: x => x.CategoryGroupId,
						principalTable: "CategoryGroups",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateIndex(
				name: "IX_Categories_CategoryGroupId_NormalizedName",
				table: "Categories",
				columns: new[] { "CategoryGroupId", "NormalizedName" },
				unique: true);

			migrationBuilder.CreateIndex(
				name: "IX_Categories_CategoryGroupId_Order",
				table: "Categories",
				columns: new[] { "CategoryGroupId", "Order" });
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropTable(
				name: "Categories");
		}
	}
}
