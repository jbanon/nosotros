using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Love.Data.Migrations
{
    /// <inheritdoc />
    public partial class Capitulos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CapituloId",
                table: "Obras",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Capitulos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Artista = table.Column<string>(type: "text", nullable: false),
                    Titulo = table.Column<string>(type: "text", nullable: false),
                    Fechas = table.Column<string>(type: "text", nullable: true),
                    Entradilla = table.Column<string>(type: "text", nullable: true),
                    Texto = table.Column<string>(type: "text", nullable: false),
                    PortadaObraId = table.Column<int>(type: "integer", nullable: true),
                    Publicacion = table.Column<DateOnly>(type: "date", nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    VistoEl = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Origen = table.Column<string>(type: "text", nullable: true),
                    Creado = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
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
