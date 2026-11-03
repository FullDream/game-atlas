using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameAtlas.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CategoryGroupColor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "CategoryGroups",
                type: "TEXT",
                maxLength: 7,
                nullable: false,
                defaultValue: "#F59E0B");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Color",
                table: "CategoryGroups");
        }
    }
}
