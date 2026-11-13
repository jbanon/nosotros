using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Love.Data.MigracionesSqlite
{
    /// <inheritdoc />
    public partial class IdeasYPeliculas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Ideas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Texto = table.Column<string>(type: "TEXT", nullable: false),
                    Nota = table.Column<string>(type: "TEXT", nullable: true),
                    Autor = table.Column<string>(type: "TEXT", nullable: false),
                    Hecha = table.Column<bool>(type: "INTEGER", nullable: false),
                    Creada = table.Column<DateTime>(type: "TEXT", nullable: false),
                    HechaEl = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ideas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Peliculas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Titulo = table.Column<string>(type: "TEXT", nullable: false),
                    Nota = table.Column<string>(type: "TEXT", nullable: true),
                    Autor = table.Column<string>(type: "TEXT", nullable: false),
                    Vista = table.Column<bool>(type: "INTEGER", nullable: false),
                    Anadida = table.Column<DateTime>(type: "TEXT", nullable: false),
                    VistaEl = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Peliculas", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Ideas");

            migrationBuilder.DropTable(
                name: "Peliculas");
        }
    }
}
