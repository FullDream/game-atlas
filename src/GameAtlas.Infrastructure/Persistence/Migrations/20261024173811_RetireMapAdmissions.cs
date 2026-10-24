using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameAtlas.Infrastructure.Persistence.Migrations
{
	/// <inheritdoc />
	public partial class RetireMapAdmissions : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.Sql("""
				CREATE TEMP TABLE "__RetireMapAdmissionGuard" (
					"Count" INTEGER NOT NULL CONSTRAINT "Media_DrainLegacyMapAdmissions"
					CHECK ("Count" = 0)
				);
				INSERT INTO "__RetireMapAdmissionGuard" SELECT
					(SELECT COUNT(*) FROM "MapAdmissionSessions") +
					(SELECT COUNT(*) FROM "MapAdmissionStyles") +
					(SELECT COUNT(*) FROM "MapCleanupIntents" WHERE "SessionId" IS NOT NULL);
				DROP TABLE "__RetireMapAdmissionGuard";
				""");

			migrationBuilder.DropColumn(name: "SessionId", table: "MapCleanupIntents");

			migrationBuilder.DropTable(
				name: "MapAdmissionStyles");

			migrationBuilder.DropTable(
				name: "MapAdmissionSessions");
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{

			migrationBuilder.AddColumn<Guid>(name: "SessionId", table: "MapCleanupIntents",
				type: "TEXT", nullable: true);
			migrationBuilder.CreateTable(
				name: "MapAdmissionSessions",
				columns: table => new
				{
					Id = table.Column<Guid>(type: "TEXT", nullable: false),
					ExpiresAt = table.Column<string>(type: "TEXT", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_MapAdmissionSessions", x => x.Id);
				});

			migrationBuilder.CreateTable(
				name: "MapAdmissionStyles",
				columns: table => new
				{
					Id = table.Column<Guid>(type: "TEXT", nullable: false),
					MapId = table.Column<Guid>(type: "TEXT", nullable: false),
					SessionId = table.Column<Guid>(type: "TEXT", nullable: false),
					StyleId = table.Column<Guid>(type: "TEXT", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_MapAdmissionStyles", x => x.Id);
					table.ForeignKey(
						name: "FK_MapAdmissionStyles_MapAdmissionSessions_SessionId",
						column: x => x.SessionId,
						principalTable: "MapAdmissionSessions",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateIndex(
				name: "IX_MapAdmissionSessions_ExpiresAt",
				table: "MapAdmissionSessions",
				column: "ExpiresAt");

			migrationBuilder.CreateIndex(
				name: "IX_MapAdmissionStyles_SessionId",
				table: "MapAdmissionStyles",
				column: "SessionId");

			migrationBuilder.CreateIndex(
				name: "IX_MapAdmissionStyles_StyleId",
				table: "MapAdmissionStyles",
				column: "StyleId",
				unique: true);
		}
	}
}
