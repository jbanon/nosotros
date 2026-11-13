using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Love.Data.MigracionesSqlite
{
    /// <inheritdoc />
    public partial class TiempoCompra : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CompraInicio",
                table: "Semanas",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DuracionMin",
                table: "Gastos",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompraInicio",
                table: "Semanas");

            migrationBuilder.DropColumn(
                name: "DuracionMin",
                table: "Gastos");
        }
    }
}
