using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Love.Data.Migrations
{
    /// <inheritdoc />
    public partial class AdvientoVideo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VideoPath",
                table: "Adviento",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VideoPath",
                table: "Adviento");
        }
    }
}
