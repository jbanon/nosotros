using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Love.Data.MigracionesSqlite
{
    /// <inheritdoc />
    public partial class AdvientoVineta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "VinetaId",
                table: "Adviento",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VinetaId",
                table: "Adviento");
        }
    }
}
