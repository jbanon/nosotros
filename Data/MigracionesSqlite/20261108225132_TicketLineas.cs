using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Love.Data.MigracionesSqlite
{
    /// <inheritdoc />
    public partial class TicketLineas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TicketLineas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GastoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Producto = table.Column<string>(type: "TEXT", nullable: false),
                    Importe = table.Column<double>(type: "REAL", nullable: true),
                    Orden = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketLineas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketLineas_Gastos_GastoId",
                        column: x => x.GastoId,
                        principalTable: "Gastos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TicketLineas_GastoId",
                table: "TicketLineas",
                column: "GastoId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketLineas_Producto",
                table: "TicketLineas",
                column: "Producto");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TicketLineas");
        }
    }
}
