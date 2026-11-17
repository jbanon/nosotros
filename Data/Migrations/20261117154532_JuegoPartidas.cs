using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Love.Data.Migrations
{
    /// <inheritdoc />
    public partial class JuegoPartidas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JuegoPartidas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    Juego = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    Inicio = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Fin = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Gano = table.Column<bool>(type: "boolean", nullable: false),
                    Palabras = table.Column<int>(type: "integer", nullable: false),
                    Total = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JuegoPartidas", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JuegoPartidas_UsuarioId_Juego_Fecha",
                table: "JuegoPartidas",
                columns: new[] { "UsuarioId", "Juego", "Fecha" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JuegoPartidas");
        }
    }
}
