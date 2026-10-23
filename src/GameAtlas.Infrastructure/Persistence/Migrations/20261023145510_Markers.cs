using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameAtlas.Infrastructure.Persistence.Migrations
{
	/// <inheritdoc />
	public partial class Markers : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.CreateTable(
				name: "Markers",
				columns: table => new
				{
					Id = table.Column<Guid>(type: "TEXT", nullable: false),
					CategoryId = table.Column<Guid>(type: "TEXT", nullable: false),
					Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
					Description = table.Column<string>(type: "TEXT", maxLength: 5000, nullable: true),
					CreatedAt = table.Column<string>(type: "TEXT", nullable: false),
					UpdatedAt = table.Column<string>(type: "TEXT", nullable: false),
					PositionX = table.Column<double>(type: "REAL", nullable: false),
					PositionY = table.Column<double>(type: "REAL", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Markers", x => x.Id);
					table.ForeignKey(
						name: "FK_Markers_Categories_CategoryId",
						column: x => x.CategoryId,
						principalTable: "Categories",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateTable(
				name: "MarkerMedia",
				columns: table => new
				{
					Id = table.Column<Guid>(type: "TEXT", nullable: false),
					MarkerId = table.Column<Guid>(type: "TEXT", nullable: false),
					Order = table.Column<int>(type: "INTEGER", nullable: false),
					Type = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
					Url = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
					Width = table.Column<int>(type: "INTEGER", nullable: true),
					Height = table.Column<int>(type: "INTEGER", nullable: true),
					Alt = table.Column<string>(type: "TEXT", nullable: true),
					Preview = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
					VideoId = table.Column<string>(type: "TEXT", maxLength: 11, nullable: true)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_MarkerMedia", x => x.Id);
					table.ForeignKey(
						name: "FK_MarkerMedia_Markers_MarkerId",
						column: x => x.MarkerId,
						principalTable: "Markers",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateIndex(
				name: "IX_MarkerMedia_MarkerId_Order",
				table: "MarkerMedia",
				columns: new[] { "MarkerId", "Order" });

			migrationBuilder.CreateIndex(
				name: "IX_Markers_CategoryId",
				table: "Markers",
				column: "CategoryId");
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropTable(
				name: "MarkerMedia");

			migrationBuilder.DropTable(
				name: "Markers");
		}
	}
}
