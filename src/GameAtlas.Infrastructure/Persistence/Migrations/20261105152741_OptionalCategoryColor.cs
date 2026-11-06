using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameAtlas.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OptionalCategoryColor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Color",
                table: "Categories",
                type: "TEXT",
                maxLength: 7,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 7);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Color",
                table: "Categories",
                type: "TEXT",
                maxLength: 7,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 7,
                oldNullable: true);
        }
    }
}
