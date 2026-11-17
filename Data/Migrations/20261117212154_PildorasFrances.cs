using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Love.Data.Migrations
{
    /// <inheritdoc />
    public partial class PildorasFrances : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EntregaId",
                table: "TareasAgente",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PildoraId",
                table: "TareasAgente",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Pildoras",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    Tema = table.Column<string>(type: "text", nullable: false),
                    Titulo = table.Column<string>(type: "text", nullable: false),
                    Emoji = table.Column<string>(type: "text", nullable: false),
                    Contenido = table.Column<string>(type: "text", nullable: false),
                    ImagenPath = table.Column<string>(type: "text", nullable: true),
                    Creada = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pildoras", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PildoraEntregas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PildoraId = table.Column<int>(type: "integer", nullable: false),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    Respuestas = table.Column<string>(type: "text", nullable: false),
                    Correccion = table.Column<string>(type: "text", nullable: true),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    Creada = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CorregidaEl = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PildoraEntregas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PildoraEntregas_Pildoras_PildoraId",
                        column: x => x.PildoraId,
                        principalTable: "Pildoras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PildoraEntregas_PildoraId_UsuarioId",
                table: "PildoraEntregas",
                columns: new[] { "PildoraId", "UsuarioId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pildoras_Fecha",
                table: "Pildoras",
                column: "Fecha");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PildoraEntregas");

            migrationBuilder.DropTable(
                name: "Pildoras");

            migrationBuilder.DropColumn(
                name: "EntregaId",
                table: "TareasAgente");

            migrationBuilder.DropColumn(
                name: "PildoraId",
                table: "TareasAgente");
        }
    }
}
