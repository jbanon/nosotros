using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Love.Data.MigracionesSqlite
{
    /// <inheritdoc />
    public partial class CapitulosSqlite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CapituloId",
                table: "Obras",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Capitulos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Artista = table.Column<string>(type: "TEXT", nullable: false),
                    Titulo = table.Column<string>(type: "TEXT", nullable: false),
                    Fechas = table.Column<string>(type: "TEXT", nullable: true),
                    Entradilla = table.Column<string>(type: "TEXT", nullable: true),
                    Texto = table.Column<string>(type: "TEXT", nullable: false),
                    PortadaObraId = table.Column<int>(type: "INTEGER", nullable: true),
                    Publicacion = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    VistoEl = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Origen = table.Column<string>(type: "TEXT", nullable: true),
                    Creado = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Capitulos", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Obras_CapituloId",
                table: "Obras",
                column: "CapituloId");

            migrationBuilder.CreateIndex(
                name: "IX_Capitulos_Origen",
                table: "Capitulos",
                column: "Origen");

            migrationBuilder.AddForeignKey(
                name: "FK_Obras_Capitulos_CapituloId",
                table: "Obras",
                column: "CapituloId",
                principalTable: "Capitulos",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Obras_Capitulos_CapituloId",
                table: "Obras");

            migrationBuilder.DropTable(
                name: "Capitulos");

            migrationBuilder.DropIndex(
                name: "IX_Obras_CapituloId",
                table: "Obras");

            migrationBuilder.DropColumn(
                name: "CapituloId",
                table: "Obras");
        }
    }
}
