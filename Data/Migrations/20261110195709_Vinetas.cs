using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Love.Data.Migrations
{
    /// <inheritdoc />
    public partial class Vinetas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "VinetaId",
                table: "TareasAgente",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Vinetas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Path = table.Column<string>(type: "TEXT", nullable: false),
                    Autor = table.Column<string>(type: "TEXT", nullable: false),
                    Libro = table.Column<string>(type: "TEXT", nullable: true),
                    FraseAutor = table.Column<string>(type: "TEXT", nullable: true),
                    FraseMia = table.Column<string>(type: "TEXT", nullable: true),
                    Intenciones = table.Column<string>(type: "TEXT", nullable: true),
                    Tono = table.Column<string>(type: "TEXT", nullable: true),
                    Momentos = table.Column<string>(type: "TEXT", nullable: true),
                    Descripcion = table.Column<string>(type: "TEXT", nullable: true),
                    JuegoSugerido = table.Column<int>(type: "INTEGER", nullable: false),
                    EspacioFrase = table.Column<bool>(type: "INTEGER", nullable: false),
                    Conocida = table.Column<bool>(type: "INTEGER", nullable: false),
                    Activa = table.Column<bool>(type: "INTEGER", nullable: false),
                    UsoPortada = table.Column<bool>(type: "INTEGER", nullable: false),
                    UsoJuego = table.Column<bool>(type: "INTEGER", nullable: false),
                    FechaFija = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Pregunta = table.Column<string>(type: "TEXT", nullable: true),
                    Respuesta = table.Column<string>(type: "TEXT", nullable: true),
                    MostradaEl = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Favorita = table.Column<bool>(type: "INTEGER", nullable: false),
                    RespuestaRaquel = table.Column<string>(type: "TEXT", nullable: true),
                    RespondidaEl = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RespuestaVista = table.Column<bool>(type: "INTEGER", nullable: false),
                    Origen = table.Column<string>(type: "TEXT", nullable: true),
                    Creada = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vinetas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VinetaDias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Fecha = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    VinetaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Juego = table.Column<int>(type: "INTEGER", nullable: false),
                    Estado = table.Column<string>(type: "TEXT", nullable: true),
                    AbiertaEl = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ResueltaEl = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Pistas = table.Column<int>(type: "INTEGER", nullable: false),
                    Corazones = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VinetaDias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VinetaDias_Vinetas_VinetaId",
                        column: x => x.VinetaId,
                        principalTable: "Vinetas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VinetaDias_Fecha",
                table: "VinetaDias",
                column: "Fecha",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VinetaDias_VinetaId",
                table: "VinetaDias",
                column: "VinetaId");

            migrationBuilder.CreateIndex(
                name: "IX_Vinetas_Origen",
                table: "Vinetas",
                column: "Origen");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VinetaDias");

            migrationBuilder.DropTable(
                name: "Vinetas");

            migrationBuilder.DropColumn(
                name: "VinetaId",
                table: "TareasAgente");
        }
    }
}
