using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Love.Data.Migrations
{
    /// <inheritdoc />
    public partial class DueloRondas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DueloRondas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Fecha = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Numeros = table.Column<string>(type: "text", nullable: false),
                    Objetivo = table.Column<int>(type: "integer", nullable: false),
                    AId = table.Column<int>(type: "integer", nullable: false),
                    AValor = table.Column<int>(type: "integer", nullable: true),
                    ASeg = table.Column<double>(type: "double precision", nullable: true),
                    BId = table.Column<int>(type: "integer", nullable: false),
                    BValor = table.Column<int>(type: "integer", nullable: true),
                    BSeg = table.Column<double>(type: "double precision", nullable: true),
                    GanadorId = table.Column<int>(type: "integer", nullable: true)
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
