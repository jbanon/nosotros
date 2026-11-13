using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Love.Data.MigracionesSqlite
{
    /// <inheritdoc />
    public partial class Adviento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Adviento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Anio = table.Column<int>(type: "INTEGER", nullable: false),
                    Dia = table.Column<int>(type: "INTEGER", nullable: false),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    Titulo = table.Column<string>(type: "TEXT", nullable: false),
                    Intro = table.Column<string>(type: "TEXT", nullable: true),
                    Datos = table.Column<string>(type: "TEXT", nullable: false),
                    Letra = table.Column<string>(type: "TEXT", nullable: true),
                    FotoPath = table.Column<string>(type: "TEXT", nullable: true),
                    Pregunta = table.Column<string>(type: "TEXT", nullable: true),
                    Respuesta = table.Column<string>(type: "TEXT", nullable: true),
                    Estado = table.Column<string>(type: "TEXT", nullable: true),
                    Pistas = table.Column<int>(type: "INTEGER", nullable: false),
                    AbiertaEl = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ResueltaEl = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CanjeadoEl = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Adviento", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Adviento_Anio_Dia",
                table: "Adviento",
                columns: new[] { "Anio", "Dia" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Adviento");
        }
    }
}
