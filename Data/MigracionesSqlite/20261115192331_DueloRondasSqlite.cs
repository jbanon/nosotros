using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Love.Data.MigracionesSqlite
{
    /// <inheritdoc />
    public partial class DueloRondasSqlite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DueloRondas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Fecha = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Numeros = table.Column<string>(type: "TEXT", nullable: false),
                    Objetivo = table.Column<int>(type: "INTEGER", nullable: false),
                    AId = table.Column<int>(type: "INTEGER", nullable: false),
                    AValor = table.Column<int>(type: "INTEGER", nullable: true),
                    ASeg = table.Column<double>(type: "REAL", nullable: true),
                    BId = table.Column<int>(type: "INTEGER", nullable: false),
                    BValor = table.Column<int>(type: "INTEGER", nullable: true),
                    BSeg = table.Column<double>(type: "REAL", nullable: true),
                    GanadorId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DueloRondas", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DueloRondas");
        }
    }
}
