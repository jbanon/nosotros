using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Love.Data.Migrations
{
    /// <inheritdoc />
    public partial class Arte : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Obras",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Titulo = table.Column<string>(type: "TEXT", nullable: false),
                    Autor = table.Column<string>(type: "TEXT", nullable: true),
                    Anio = table.Column<string>(type: "TEXT", nullable: true),
                    Museo = table.Column<string>(type: "TEXT", nullable: true),
                    FuenteUrl = table.Column<string>(type: "TEXT", nullable: true),
                    Licencia = table.Column<string>(type: "TEXT", nullable: true),
                    Intro = table.Column<string>(type: "TEXT", nullable: true),
                    FraseMia = table.Column<string>(type: "TEXT", nullable: true),
                    ImagenPath = table.Column<string>(type: "TEXT", nullable: true),
                    ReducidaPath = table.Column<string>(type: "TEXT", nullable: true),
                    Ancho = table.Column<int>(type: "INTEGER", nullable: false),
                    Alto = table.Column<int>(type: "INTEGER", nullable: false),
                    Publicacion = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Orden = table.Column<int>(type: "INTEGER", nullable: false),
                    Activa = table.Column<bool>(type: "INTEGER", nullable: false),
                    VistaEl = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Favorita = table.Column<bool>(type: "INTEGER", nullable: false),
                    RespuestaRaquel = table.Column<string>(type: "TEXT", nullable: true),
                    RespondidaEl = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RespuestaVista = table.Column<bool>(type: "INTEGER", nullable: false),
                    Origen = table.Column<string>(type: "TEXT", nullable: true),
                    Creada = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Obras", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ParadasObra",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ObraId = table.Column<int>(type: "INTEGER", nullable: false),
                    Orden = table.Column<int>(type: "INTEGER", nullable: false),
                    X = table.Column<double>(type: "REAL", nullable: false),
                    Y = table.Column<double>(type: "REAL", nullable: false),
                    Zoom = table.Column<double>(type: "REAL", nullable: false),
                    Titulo = table.Column<string>(type: "TEXT", nullable: false),
                    Texto = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParadasObra", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParadasObra_Obras_ObraId",
                        column: x => x.ObraId,
                        principalTable: "Obras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Obras_Origen",
                table: "Obras",
                column: "Origen");

            migrationBuilder.CreateIndex(
                name: "IX_ParadasObra_ObraId",
                table: "ParadasObra",
                column: "ObraId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ParadasObra");

            migrationBuilder.DropTable(
                name: "Obras");
        }
    }
}
