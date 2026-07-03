using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ADS2026.Migrations
{
    /// <inheritdoc />
    public partial class AddTitleDescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "MediaFiles",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "MediaFiles",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "MediaFiles");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "MediaFiles");
        }
    }
}
