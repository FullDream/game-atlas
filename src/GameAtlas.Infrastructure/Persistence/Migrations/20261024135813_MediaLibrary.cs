using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameAtlas.Infrastructure.Persistence.Migrations
{
	/// <inheritdoc />
	public partial class MediaLibrary : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.CreateTable(
				name: "MediaFolders",
				columns: table => new
				{
					Id = table.Column<Guid>(type: "TEXT", nullable: false),
					ParentId = table.Column<Guid>(type: "TEXT", nullable: true),
					Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
					NormalizedName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
					CreatedAt = table.Column<string>(type: "TEXT", nullable: false),
					UpdatedAt = table.Column<string>(type: "TEXT", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_MediaFolders", x => x.Id);
					table.ForeignKey(
						name: "FK_MediaFolders_MediaFolders_ParentId",
						column: x => x.ParentId,
						principalTable: "MediaFolders",
						principalColumn: "Id",
						onDelete: ReferentialAction.Restrict);
				});

			migrationBuilder.CreateTable(
				name: "MediaAssets",
				columns: table => new
				{
					Id = table.Column<Guid>(type: "TEXT", nullable: false),
					FolderId = table.Column<Guid>(type: "TEXT", nullable: true),
					Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
					ContentType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
					ByteLength = table.Column<long>(type: "INTEGER", nullable: false),
					CreatedAt = table.Column<string>(type: "TEXT", nullable: false),
					UpdatedAt = table.Column<string>(type: "TEXT", nullable: false),
					Type = table.Column<string>(type: "TEXT", maxLength: 13, nullable: false),
					Width = table.Column<double>(type: "REAL", nullable: true),
					Height = table.Column<double>(type: "REAL", nullable: true),
					Alt = table.Column<string>(type: "TEXT", nullable: true),
					Placeholder = table.Column<string>(type: "TEXT", nullable: true),
					PreviewAssetId = table.Column<Guid>(type: "TEXT", nullable: true)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_MediaAssets", x => x.Id);
					table.ForeignKey(
						name: "FK_MediaAssets_MediaAssets_PreviewAssetId",
						column: x => x.PreviewAssetId,
						principalTable: "MediaAssets",
						principalColumn: "Id",
						onDelete: ReferentialAction.Restrict);
					table.ForeignKey(
						name: "FK_MediaAssets_MediaFolders_FolderId",
						column: x => x.FolderId,
						principalTable: "MediaFolders",
						principalColumn: "Id",
						onDelete: ReferentialAction.Restrict);
				});

			migrationBuilder.CreateIndex(
				name: "IX_MediaAssets_FolderId_Name_Id",
				table: "MediaAssets",
				columns: new[] { "FolderId", "Name", "Id" });

			migrationBuilder.CreateIndex(
				name: "IX_MediaAssets_PreviewAssetId",
				table: "MediaAssets",
				column: "PreviewAssetId");

			migrationBuilder.CreateIndex(
				name: "IX_MediaFolders_NormalizedName",
				table: "MediaFolders",
				column: "NormalizedName",
				unique: true,
				filter: "ParentId IS NULL");

			migrationBuilder.CreateIndex(
				name: "IX_MediaFolders_ParentId_Name_Id",
				table: "MediaFolders",
				columns: new[] { "ParentId", "Name", "Id" });

			migrationBuilder.CreateIndex(
				name: "IX_MediaFolders_ParentId_NormalizedName",
				table: "MediaFolders",
				columns: new[] { "ParentId", "NormalizedName" },
				unique: true);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropTable(
				name: "MediaAssets");

			migrationBuilder.DropTable(
				name: "MediaFolders");
		}
	}
}
