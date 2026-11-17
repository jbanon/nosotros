using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Love.Data.MigracionesSqlite
{
    /// <inheritdoc />
    public partial class PildorasFrancesSqlite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EntregaId",
                table: "TareasAgente",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PildoraId",
                table: "TareasAgente",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Pildoras",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Fecha = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Tema = table.Column<string>(type: "TEXT", nullable: false),
                    Titulo = table.Column<string>(type: "TEXT", nullable: false),
                    Emoji = table.Column<string>(type: "TEXT", nullable: false),
                    Contenido = table.Column<string>(type: "TEXT", nullable: false),
                    ImagenPath = table.Column<string>(type: "TEXT", nullable: true),
                    Creada = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pildoras", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PildoraEntregas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PildoraId = table.Column<int>(type: "INTEGER", nullable: false),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    Respuestas = table.Column<string>(type: "TEXT", nullable: false),
                    Correccion = table.Column<string>(type: "TEXT", nullable: true),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    Creada = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CorregidaEl = table.Column<DateTime>(type: "TEXT", nullable: true)
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
