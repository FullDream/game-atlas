using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameAtlas.Infrastructure.Persistence.Migrations
{
	/// <inheritdoc />
	public partial class MapStylePresentation : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.AddColumn<long>(
				name: "DisplayOrder",
				table: "MapStyles",
				type: "INTEGER",
				nullable: false,
				defaultValue: 0L);

			migrationBuilder.AlterColumn<int>(
				name: "Width",
				table: "Maps",
				type: "INTEGER",
				nullable: true,
				oldClrType: typeof(int),
				oldType: "INTEGER");

			migrationBuilder.AlterColumn<int>(
				name: "Height",
				table: "Maps",
				type: "INTEGER",
				nullable: true,
				oldClrType: typeof(int),
				oldType: "INTEGER");

			migrationBuilder.AddColumn<Guid>(
				name: "DefaultStyleId",
				table: "Maps",
				type: "TEXT",
				nullable: true);
			migrationBuilder.Sql("""
				UPDATE MapStyles SET DisplayOrder = CreationSequence;
				UPDATE Maps SET DefaultStyleId = (
					SELECT Id FROM MapStyles WHERE MapId = Maps.Id AND Status = 'Ready'
					ORDER BY CreationSequence LIMIT 1);
				""");
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.Sql("""
				CREATE TABLE "__MapStylePresentationRollback" (
					Value INTEGER CONSTRAINT MapStylePresentation_RestoreBackupForNonemptyMaps CHECK (Value = 0));
				INSERT INTO "__MapStylePresentationRollback" SELECT COUNT(*) FROM Maps;
				DROP TABLE "__MapStylePresentationRollback";
				""");

			migrationBuilder.DropColumn(
				name: "DisplayOrder",
				table: "MapStyles");

			migrationBuilder.DropColumn(
				name: "DefaultStyleId",
				table: "Maps");

			migrationBuilder.AlterColumn<int>(
				name: "Width",
				table: "Maps",
				type: "INTEGER",
				nullable: false,
				defaultValue: 0,
				oldClrType: typeof(int),
				oldType: "INTEGER",
				oldNullable: true);

			migrationBuilder.AlterColumn<int>(
				name: "Height",
				table: "Maps",
				type: "INTEGER",
				nullable: false,
				defaultValue: 0,
				oldClrType: typeof(int),
				oldType: "INTEGER",
				oldNullable: true);
		}
	}
}
