using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Love.Data.Migrations
{
    /// <inheritdoc />
    public partial class IdeaResumen : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Resumen",
                table: "Ideas",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Resumen",
                table: "Ideas");
        }
    }
}
