using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Love.Data.Migrations
{
    /// <inheritdoc />
    public partial class Agente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Anio",
                table: "Peliculas",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Curiosidad",
                table: "Peliculas",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Director",
                table: "Peliculas",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Duracion",
                table: "Peliculas",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FichaEl",
                table: "Peliculas",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Genero",
                table: "Peliculas",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PosterPath",
                table: "Peliculas",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Reparto",
                table: "Peliculas",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Sinopsis",
                table: "Peliculas",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TituloOriginal",
                table: "Peliculas",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TareasAgente",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    Entrada = table.Column<string>(type: "TEXT", nullable: false),
                    Notas = table.Column<string>(type: "TEXT", nullable: true),
                    FotoPath = table.Column<string>(type: "TEXT", nullable: true),
                    Propuesta = table.Column<string>(type: "TEXT", nullable: true),
                    Historial = table.Column<string>(type: "TEXT", nullable: false),
                    Error = table.Column<string>(type: "TEXT", nullable: true),
                    Autor = table.Column<string>(type: "TEXT", nullable: false),
                    PeliculaId = table.Column<int>(type: "INTEGER", nullable: true),
                    RecetaId = table.Column<int>(type: "INTEGER", nullable: true),
                    Rondas = table.Column<int>(type: "INTEGER", nullable: false),
                    DuracionMs = table.Column<int>(type: "INTEGER", nullable: false),
                    CosteUsd = table.Column<double>(type: "REAL", nullable: false),
                    Creada = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Terminada = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Aplicada = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TareasAgente", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TareasAgente_Estado",
                table: "TareasAgente",
                column: "Estado");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TareasAgente");

            migrationBuilder.DropColumn(
                name: "Anio",
                table: "Peliculas");

            migrationBuilder.DropColumn(
                name: "Curiosidad",
                table: "Peliculas");

            migrationBuilder.DropColumn(
                name: "Director",
                table: "Peliculas");

            migrationBuilder.DropColumn(
                name: "Duracion",
                table: "Peliculas");

            migrationBuilder.DropColumn(
                name: "FichaEl",
                table: "Peliculas");

            migrationBuilder.DropColumn(
                name: "Genero",
                table: "Peliculas");

            migrationBuilder.DropColumn(
                name: "PosterPath",
                table: "Peliculas");

            migrationBuilder.DropColumn(
                name: "Reparto",
                table: "Peliculas");

            migrationBuilder.DropColumn(
                name: "Sinopsis",
                table: "Peliculas");

            migrationBuilder.DropColumn(
                name: "TituloOriginal",
                table: "Peliculas");
        }
    }
}
