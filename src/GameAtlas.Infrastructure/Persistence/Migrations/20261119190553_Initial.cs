using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GameAtlas.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class _20261119190553_Initial : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AspNetUsers",
            columns: table => new
            {
                Id = table.Column<string>(type: "text", nullable: false),
                UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                PasswordHash = table.Column<string>(type: "text", nullable: true),
                SecurityStamp = table.Column<string>(type: "text", nullable: true),
                ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                PhoneNumber = table.Column<string>(type: "text", nullable: true),
                PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetUsers", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "MapCleanupIntents",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                MapId = table.Column<Guid>(type: "uuid", nullable: true),
                StyleId = table.Column<Guid>(type: "uuid", nullable: true),
                OutputsOnly = table.Column<bool>(type: "boolean", nullable: false),
                Attempts = table.Column<int>(type: "integer", nullable: false),
                RetryAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MapCleanupIntents", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "MediaFolders",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                NormalizedName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
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
            name: "AspNetUserClaims",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                UserId = table.Column<string>(type: "text", nullable: false),
                ClaimType = table.Column<string>(type: "text", nullable: true),
                ClaimValue = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                table.ForeignKey(
                    name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "AspNetUserLogins",
            columns: table => new
            {
                LoginProvider = table.Column<string>(type: "text", nullable: false),
                ProviderKey = table.Column<string>(type: "text", nullable: false),
                ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                UserId = table.Column<string>(type: "text", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                table.ForeignKey(
                    name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "AspNetUserTokens",
            columns: table => new
            {
                UserId = table.Column<string>(type: "text", nullable: false),
                LoginProvider = table.Column<string>(type: "text", nullable: false),
                Name = table.Column<string>(type: "text", nullable: false),
                Value = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                table.ForeignKey(
                    name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "MediaAssets",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                FolderId = table.Column<Guid>(type: "uuid", nullable: true),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                ContentType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                ByteLength = table.Column<long>(type: "bigint", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                Type = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                Width = table.Column<double>(type: "double precision", nullable: true),
                Height = table.Column<double>(type: "double precision", nullable: true),
                Alt = table.Column<string>(type: "text", nullable: true),
                Placeholder = table.Column<string>(type: "text", nullable: true),
                PreviewAssetId = table.Column<Guid>(type: "uuid", nullable: true)
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

        migrationBuilder.CreateTable(
            name: "Games",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                CoverId = table.Column<Guid>(type: "uuid", nullable: false),
                BannerId = table.Column<Guid>(type: "uuid", nullable: true),
                ReleaseDate = table.Column<DateOnly>(type: "date", nullable: true),
                Developer = table.Column<string>(type: "text", nullable: true),
                Description = table.Column<string>(type: "text", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                Status = table.Column<string>(type: "text", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Games", x => x.Id);
                table.ForeignKey(
                    name: "FK_Games_MediaAssets_BannerId",
                    column: x => x.BannerId,
                    principalTable: "MediaAssets",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Games_MediaAssets_CoverId",
                    column: x => x.CoverId,
                    principalTable: "MediaAssets",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Maps",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                GameId = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                NormalizedName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Width = table.Column<int>(type: "integer", nullable: true),
                Height = table.Column<int>(type: "integer", nullable: true),
                LastCreationSequence = table.Column<long>(type: "bigint", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                DefaultStyleId = table.Column<Guid>(type: "uuid", nullable: true),
                Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Maps", x => x.Id);
                table.ForeignKey(
                    name: "FK_Maps_Games_GameId",
                    column: x => x.GameId,
                    principalTable: "Games",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "CategoryGroups",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                MapId = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                NormalizedName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false, defaultValue: "#F59E0B"),
                Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                Order = table.Column<int>(type: "integer", nullable: false),
                IconAssetId = table.Column<Guid>(type: "uuid", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CategoryGroups", x => x.Id);
                table.ForeignKey(
                    name: "FK_CategoryGroups_Maps_MapId",
                    column: x => x.MapId,
                    principalTable: "Maps",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_CategoryGroups_MediaAssets_IconAssetId",
                    column: x => x.IconAssetId,
                    principalTable: "MediaAssets",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "MapStyles",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                MapId = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                NormalizedName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                BackgroundColor = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                CreationSequence = table.Column<long>(type: "bigint", nullable: false),
                DisplayOrder = table.Column<long>(type: "bigint", nullable: false),
                SourceAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                Status = table.Column<string>(type: "text", nullable: false),
                ErrorCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                ErrorMessage = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                AttemptId = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MapStyles", x => x.Id);
                table.ForeignKey(
                    name: "FK_MapStyles_Maps_MapId",
                    column: x => x.MapId,
                    principalTable: "Maps",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_MapStyles_MediaAssets_SourceAssetId",
                    column: x => x.SourceAssetId,
                    principalTable: "MediaAssets",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Categories",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CategoryGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                NormalizedName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                IconAssetId = table.Column<Guid>(type: "uuid", nullable: true),
                Color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                Order = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
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
                table.ForeignKey(
                    name: "FK_Categories_MediaAssets_IconAssetId",
                    column: x => x.IconAssetId,
                    principalTable: "MediaAssets",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Markers",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                PositionX = table.Column<double>(type: "double precision", nullable: false),
                PositionY = table.Column<double>(type: "double precision", nullable: false)
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
            name: "MarkerAssets",
            columns: table => new
            {
                MarkerId = table.Column<Guid>(type: "uuid", nullable: false),
                AssetId = table.Column<Guid>(type: "uuid", nullable: false),
                Order = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MarkerAssets", x => new { x.MarkerId, x.AssetId });
                table.ForeignKey(
                    name: "FK_MarkerAssets_Markers_MarkerId",
                    column: x => x.MarkerId,
                    principalTable: "Markers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_MarkerAssets_MediaAssets_AssetId",
                    column: x => x.AssetId,
                    principalTable: "MediaAssets",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AspNetUserClaims_UserId",
            table: "AspNetUserClaims",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_AspNetUserLogins_UserId",
            table: "AspNetUserLogins",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "EmailIndex",
            table: "AspNetUsers",
            column: "NormalizedEmail");

        migrationBuilder.CreateIndex(
            name: "UserNameIndex",
            table: "AspNetUsers",
            column: "NormalizedUserName",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Categories_CategoryGroupId_NormalizedName",
            table: "Categories",
            columns: new[] { "CategoryGroupId", "NormalizedName" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Categories_CategoryGroupId_Order",
            table: "Categories",
            columns: new[] { "CategoryGroupId", "Order" });

        migrationBuilder.CreateIndex(
            name: "IX_Categories_IconAssetId",
            table: "Categories",
            column: "IconAssetId");

        migrationBuilder.CreateIndex(
            name: "IX_CategoryGroups_IconAssetId",
            table: "CategoryGroups",
            column: "IconAssetId");

        migrationBuilder.CreateIndex(
            name: "IX_CategoryGroups_MapId_NormalizedName",
            table: "CategoryGroups",
            columns: new[] { "MapId", "NormalizedName" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_CategoryGroups_MapId_Order",
            table: "CategoryGroups",
            columns: new[] { "MapId", "Order" });

        migrationBuilder.CreateIndex(
            name: "IX_Games_BannerId",
            table: "Games",
            column: "BannerId");

        migrationBuilder.CreateIndex(
            name: "IX_Games_CoverId",
            table: "Games",
            column: "CoverId");

        migrationBuilder.CreateIndex(
            name: "IX_Games_Slug",
            table: "Games",
            column: "Slug",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_MapCleanupIntents_RetryAt",
            table: "MapCleanupIntents",
            column: "RetryAt");

        migrationBuilder.CreateIndex(
            name: "IX_Maps_GameId",
            table: "Maps",
            column: "GameId");

        migrationBuilder.CreateIndex(
            name: "IX_Maps_GameId_NormalizedName",
            table: "Maps",
            columns: new[] { "GameId", "NormalizedName" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Maps_GameId_Slug",
            table: "Maps",
            columns: new[] { "GameId", "Slug" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_MapStyles_MapId_CreationSequence",
            table: "MapStyles",
            columns: new[] { "MapId", "CreationSequence" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_MapStyles_MapId_NormalizedName",
            table: "MapStyles",
            columns: new[] { "MapId", "NormalizedName" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_MapStyles_SourceAssetId",
            table: "MapStyles",
            column: "SourceAssetId");

        migrationBuilder.CreateIndex(
            name: "IX_MapStyles_Status_CreatedAt",
            table: "MapStyles",
            columns: new[] { "Status", "CreatedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_MarkerAssets_AssetId",
            table: "MarkerAssets",
            column: "AssetId");

        migrationBuilder.CreateIndex(
            name: "IX_MarkerAssets_MarkerId_Order",
            table: "MarkerAssets",
            columns: new[] { "MarkerId", "Order" });

        migrationBuilder.CreateIndex(
            name: "IX_Markers_CategoryId",
            table: "Markers",
            column: "CategoryId");

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
            filter: "\"ParentId\" IS NULL");

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
            name: "AspNetUserClaims");

        migrationBuilder.DropTable(
            name: "AspNetUserLogins");

        migrationBuilder.DropTable(
            name: "AspNetUserTokens");

        migrationBuilder.DropTable(
            name: "MapCleanupIntents");

        migrationBuilder.DropTable(
            name: "MapStyles");

        migrationBuilder.DropTable(
            name: "MarkerAssets");

        migrationBuilder.DropTable(
            name: "AspNetUsers");

        migrationBuilder.DropTable(
            name: "Markers");

        migrationBuilder.DropTable(
            name: "Categories");

        migrationBuilder.DropTable(
            name: "CategoryGroups");

        migrationBuilder.DropTable(
            name: "Maps");

        migrationBuilder.DropTable(
            name: "Games");

        migrationBuilder.DropTable(
            name: "MediaAssets");

        migrationBuilder.DropTable(
            name: "MediaFolders");
    }
}
