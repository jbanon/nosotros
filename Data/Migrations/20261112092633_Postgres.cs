using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Love.Data.Migrations
{
    /// <inheritdoc />
    public partial class Postgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Adviento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Anio = table.Column<int>(type: "integer", nullable: false),
                    Dia = table.Column<int>(type: "integer", nullable: false),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    Titulo = table.Column<string>(type: "text", nullable: false),
                    Intro = table.Column<string>(type: "text", nullable: true),
                    Datos = table.Column<string>(type: "text", nullable: false),
                    Letra = table.Column<string>(type: "text", nullable: true),
                    FotoPath = table.Column<string>(type: "text", nullable: true),
                    VideoPath = table.Column<string>(type: "text", nullable: true),
                    VinetaId = table.Column<int>(type: "integer", nullable: true),
                    Pregunta = table.Column<string>(type: "text", nullable: true),
                    Respuesta = table.Column<string>(type: "text", nullable: true),
                    Estado = table.Column<string>(type: "text", nullable: true),
                    Pistas = table.Column<int>(type: "integer", nullable: false),
                    AbiertaEl = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ResueltaEl = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CanjeadoEl = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Adviento", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Configs",
                columns: table => new
                {
                    Clave = table.Column<string>(type: "text", nullable: false),
                    Valor = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Configs", x => x.Clave);
                });

            migrationBuilder.CreateTable(
                name: "FechasEspeciales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Mes = table.Column<int>(type: "integer", nullable: false),
                    Dia = table.Column<int>(type: "integer", nullable: false),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Texto = table.Column<string>(type: "text", nullable: false),
                    AnioOrigen = table.Column<int>(type: "integer", nullable: true),
                    TextoContador = table.Column<string>(type: "text", nullable: true),
                    Tema = table.Column<string>(type: "text", nullable: true),
                    Emoji = table.Column<string>(type: "text", nullable: true),
                    CuentaAtras = table.Column<bool>(type: "boolean", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FechasEspeciales", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Gastos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    Importe = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Nota = table.Column<string>(type: "text", nullable: true),
                    AdjuntoPath = table.Column<string>(type: "text", nullable: true),
                    AdjuntoNombre = table.Column<string>(type: "text", nullable: true),
                    AdjuntoMime = table.Column<string>(type: "text", nullable: true),
                    EsFijo = table.Column<bool>(type: "boolean", nullable: false),
                    DuracionMin = table.Column<int>(type: "integer", nullable: true),
                    Creado = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Gastos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Ideas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Texto = table.Column<string>(type: "text", nullable: false),
                    Nota = table.Column<string>(type: "text", nullable: true),
                    Autor = table.Column<string>(type: "text", nullable: false),
                    Hecha = table.Column<bool>(type: "boolean", nullable: false),
                    Creada = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    HechaEl = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Resumen = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ideas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Ingredientes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Zona = table.Column<int>(type: "integer", nullable: false),
                    Despensa = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ingredientes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lecturas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Titulo = table.Column<string>(type: "text", nullable: false),
                    Autor = table.Column<string>(type: "text", nullable: true),
                    Fuente = table.Column<string>(type: "text", nullable: true),
                    Dedicatoria = table.Column<string>(type: "text", nullable: true),
                    Texto = table.Column<string>(type: "text", nullable: false),
                    Publicada = table.Column<bool>(type: "boolean", nullable: false),
                    Leida = table.Column<bool>(type: "boolean", nullable: false),
                    Creada = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LeidaEl = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lecturas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Listas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Emoji = table.Column<string>(type: "text", nullable: true),
                    Creada = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Listas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Mensajes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Mes = table.Column<int>(type: "integer", nullable: false),
                    Dia = table.Column<int>(type: "integer", nullable: false),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    Texto = table.Column<string>(type: "text", nullable: false),
                    Titulo = table.Column<string>(type: "text", nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mensajes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MesesInicializados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Anio = table.Column<int>(type: "integer", nullable: false),
                    Mes = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MesesInicializados", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Obras",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Titulo = table.Column<string>(type: "text", nullable: false),
                    Autor = table.Column<string>(type: "text", nullable: true),
                    Anio = table.Column<string>(type: "text", nullable: true),
                    Museo = table.Column<string>(type: "text", nullable: true),
                    FuenteUrl = table.Column<string>(type: "text", nullable: true),
                    Licencia = table.Column<string>(type: "text", nullable: true),
                    Intro = table.Column<string>(type: "text", nullable: true),
                    FraseMia = table.Column<string>(type: "text", nullable: true),
                    ImagenPath = table.Column<string>(type: "text", nullable: true),
                    ReducidaPath = table.Column<string>(type: "text", nullable: true),
                    Ancho = table.Column<int>(type: "integer", nullable: false),
                    Alto = table.Column<int>(type: "integer", nullable: false),
                    Publicacion = table.Column<DateOnly>(type: "date", nullable: true),
                    Orden = table.Column<int>(type: "integer", nullable: false),
                    Activa = table.Column<bool>(type: "boolean", nullable: false),
                    VistaEl = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Favorita = table.Column<bool>(type: "boolean", nullable: false),
                    RespuestaRaquel = table.Column<string>(type: "text", nullable: true),
                    RespondidaEl = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    RespuestaVista = table.Column<bool>(type: "boolean", nullable: false),
                    Origen = table.Column<string>(type: "text", nullable: true),
                    Creada = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Obras", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Peliculas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Titulo = table.Column<string>(type: "text", nullable: false),
                    Nota = table.Column<string>(type: "text", nullable: true),
                    Autor = table.Column<string>(type: "text", nullable: false),
                    Vista = table.Column<bool>(type: "boolean", nullable: false),
                    Anadida = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    VistaEl = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    TituloOriginal = table.Column<string>(type: "text", nullable: true),
                    Anio = table.Column<int>(type: "integer", nullable: true),
                    Director = table.Column<string>(type: "text", nullable: true),
                    Reparto = table.Column<string>(type: "text", nullable: true),
                    Genero = table.Column<string>(type: "text", nullable: true),
                    Duracion = table.Column<int>(type: "integer", nullable: true),
                    Sinopsis = table.Column<string>(type: "text", nullable: true),
                    Curiosidad = table.Column<string>(type: "text", nullable: true),
                    PosterPath = table.Column<string>(type: "text", nullable: true),
                    FichaEl = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Peliculas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Recetas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Descripcion = table.Column<string>(type: "text", nullable: true),
                    Raciones = table.Column<int>(type: "integer", nullable: false),
                    Etiquetas = table.Column<string>(type: "text", nullable: true),
                    Preparacion = table.Column<string>(type: "text", nullable: true),
                    FotoPath = table.Column<string>(type: "text", nullable: true),
                    Creada = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recetas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Semanas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LunesInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    Notas = table.Column<string>(type: "text", nullable: true),
                    CompraInicio = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Semanas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TareasAgente",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    Entrada = table.Column<string>(type: "text", nullable: false),
                    Notas = table.Column<string>(type: "text", nullable: true),
                    FotoPath = table.Column<string>(type: "text", nullable: true),
                    Propuesta = table.Column<string>(type: "text", nullable: true),
                    Historial = table.Column<string>(type: "text", nullable: false),
                    Error = table.Column<string>(type: "text", nullable: true),
                    Autor = table.Column<string>(type: "text", nullable: false),
                    PeliculaId = table.Column<int>(type: "integer", nullable: true),
                    RecetaId = table.Column<int>(type: "integer", nullable: true),
                    VinetaId = table.Column<int>(type: "integer", nullable: true),
                    Rondas = table.Column<int>(type: "integer", nullable: false),
                    DuracionMs = table.Column<int>(type: "integer", nullable: false),
                    CosteUsd = table.Column<decimal>(type: "numeric(12,6)", precision: 12, scale: 6, nullable: false),
                    Creada = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Terminada = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Aplicada = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TareasAgente", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    PinHash = table.Column<string>(type: "text", nullable: false),
                    Rol = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Vinetas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Path = table.Column<string>(type: "text", nullable: false),
                    Autor = table.Column<string>(type: "text", nullable: false),
                    Libro = table.Column<string>(type: "text", nullable: true),
                    FraseAutor = table.Column<string>(type: "text", nullable: true),
                    FraseMia = table.Column<string>(type: "text", nullable: true),
                    Intenciones = table.Column<string>(type: "text", nullable: true),
                    Tono = table.Column<string>(type: "text", nullable: true),
                    Momentos = table.Column<string>(type: "text", nullable: true),
                    Descripcion = table.Column<string>(type: "text", nullable: true),
                    JuegoSugerido = table.Column<int>(type: "integer", nullable: false),
                    EspacioFrase = table.Column<bool>(type: "boolean", nullable: false),
                    Conocida = table.Column<bool>(type: "boolean", nullable: false),
                    Activa = table.Column<bool>(type: "boolean", nullable: false),
                    UsoPortada = table.Column<bool>(type: "boolean", nullable: false),
                    UsoJuego = table.Column<bool>(type: "boolean", nullable: false),
                    FechaFija = table.Column<DateOnly>(type: "date", nullable: true),
                    Pregunta = table.Column<string>(type: "text", nullable: true),
                    Respuesta = table.Column<string>(type: "text", nullable: true),
                    MostradaEl = table.Column<DateOnly>(type: "date", nullable: true),
                    Favorita = table.Column<bool>(type: "boolean", nullable: false),
                    RespuestaRaquel = table.Column<string>(type: "text", nullable: true),
                    RespondidaEl = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    RespuestaVista = table.Column<bool>(type: "boolean", nullable: false),
                    Origen = table.Column<string>(type: "text", nullable: true),
                    Creada = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vinetas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TicketLineas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GastoId = table.Column<int>(type: "integer", nullable: false),
                    Producto = table.Column<string>(type: "text", nullable: false),
                    Importe = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    Orden = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketLineas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketLineas_Gastos_GastoId",
                        column: x => x.GastoId,
                        principalTable: "Gastos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ListaElementos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ListaId = table.Column<int>(type: "integer", nullable: false),
                    Texto = table.Column<string>(type: "text", nullable: false),
                    Hecho = table.Column<bool>(type: "boolean", nullable: false),
                    Orden = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListaElementos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ListaElementos_Listas_ListaId",
                        column: x => x.ListaId,
                        principalTable: "Listas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ParadasObra",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ObraId = table.Column<int>(type: "integer", nullable: false),
                    Orden = table.Column<int>(type: "integer", nullable: false),
                    X = table.Column<double>(type: "double precision", nullable: false),
                    Y = table.Column<double>(type: "double precision", nullable: false),
                    Zoom = table.Column<double>(type: "double precision", nullable: false),
                    Titulo = table.Column<string>(type: "text", nullable: false),
                    Texto = table.Column<string>(type: "text", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "RecetaIngredientes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RecetaId = table.Column<int>(type: "integer", nullable: false),
                    IngredienteId = table.Column<int>(type: "integer", nullable: false),
                    Cantidad = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: true),
                    Unidad = table.Column<string>(type: "text", nullable: true),
                    Nota = table.Column<string>(type: "text", nullable: true),
                    Orden = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecetaIngredientes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecetaIngredientes_Ingredientes_IngredienteId",
                        column: x => x.IngredienteId,
                        principalTable: "Ingredientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RecetaIngredientes_Recetas_RecetaId",
                        column: x => x.RecetaId,
                        principalTable: "Recetas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ListaItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SemanaId = table.Column<int>(type: "integer", nullable: false),
                    IngredienteId = table.Column<int>(type: "integer", nullable: true),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Cantidad = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: true),
                    Unidad = table.Column<string>(type: "text", nullable: true),
                    Zona = table.Column<int>(type: "integer", nullable: false),
                    Incluido = table.Column<bool>(type: "boolean", nullable: false),
                    Comprado = table.Column<bool>(type: "boolean", nullable: false),
                    Manual = table.Column<bool>(type: "boolean", nullable: false),
                    Origen = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListaItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ListaItems_Semanas_SemanaId",
                        column: x => x.SemanaId,
                        principalTable: "Semanas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SemanaRecetas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SemanaId = table.Column<int>(type: "integer", nullable: false),
                    RecetaId = table.Column<int>(type: "integer", nullable: false),
                    Veces = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SemanaRecetas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SemanaRecetas_Recetas_RecetaId",
                        column: x => x.RecetaId,
                        principalTable: "Recetas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SemanaRecetas_Semanas_SemanaId",
                        column: x => x.SemanaId,
                        principalTable: "Semanas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VinetaDias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    VinetaId = table.Column<int>(type: "integer", nullable: false),
                    Juego = table.Column<int>(type: "integer", nullable: false),
                    Estado = table.Column<string>(type: "text", nullable: true),
                    AbiertaEl = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ResueltaEl = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Pistas = table.Column<int>(type: "integer", nullable: false),
                    Corazones = table.Column<int>(type: "integer", nullable: false)
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
                name: "IX_Adviento_Anio_Dia",
                table: "Adviento",
                columns: new[] { "Anio", "Dia" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Gastos_Fecha",
                table: "Gastos",
                column: "Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_Ingredientes_Nombre",
                table: "Ingredientes",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ListaElementos_ListaId",
                table: "ListaElementos",
                column: "ListaId");

            migrationBuilder.CreateIndex(
                name: "IX_ListaItems_SemanaId",
                table: "ListaItems",
                column: "SemanaId");

            migrationBuilder.CreateIndex(
                name: "IX_Mensajes_Mes_Dia",
                table: "Mensajes",
                columns: new[] { "Mes", "Dia" });

            migrationBuilder.CreateIndex(
                name: "IX_MesesInicializados_Anio_Mes",
                table: "MesesInicializados",
                columns: new[] { "Anio", "Mes" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Obras_Origen",
                table: "Obras",
                column: "Origen");

            migrationBuilder.CreateIndex(
                name: "IX_ParadasObra_ObraId",
                table: "ParadasObra",
                column: "ObraId");

            migrationBuilder.CreateIndex(
                name: "IX_RecetaIngredientes_IngredienteId",
                table: "RecetaIngredientes",
                column: "IngredienteId");

            migrationBuilder.CreateIndex(
                name: "IX_RecetaIngredientes_RecetaId",
                table: "RecetaIngredientes",
                column: "RecetaId");

            migrationBuilder.CreateIndex(
                name: "IX_SemanaRecetas_RecetaId",
                table: "SemanaRecetas",
                column: "RecetaId");

            migrationBuilder.CreateIndex(
                name: "IX_SemanaRecetas_SemanaId",
                table: "SemanaRecetas",
                column: "SemanaId");

            migrationBuilder.CreateIndex(
                name: "IX_Semanas_LunesInicio",
                table: "Semanas",
                column: "LunesInicio",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TareasAgente_Estado",
                table: "TareasAgente",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_TicketLineas_GastoId",
                table: "TicketLineas",
                column: "GastoId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketLineas_Producto",
                table: "TicketLineas",
                column: "Producto");

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
                name: "Adviento");

            migrationBuilder.DropTable(
                name: "Configs");

            migrationBuilder.DropTable(
                name: "FechasEspeciales");

            migrationBuilder.DropTable(
                name: "Ideas");

            migrationBuilder.DropTable(
                name: "Lecturas");

            migrationBuilder.DropTable(
                name: "ListaElementos");

            migrationBuilder.DropTable(
                name: "ListaItems");

            migrationBuilder.DropTable(
                name: "Mensajes");

            migrationBuilder.DropTable(
                name: "MesesInicializados");

            migrationBuilder.DropTable(
                name: "ParadasObra");

            migrationBuilder.DropTable(
                name: "Peliculas");

            migrationBuilder.DropTable(
                name: "RecetaIngredientes");

            migrationBuilder.DropTable(
                name: "SemanaRecetas");

            migrationBuilder.DropTable(
                name: "TareasAgente");

            migrationBuilder.DropTable(
                name: "TicketLineas");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "VinetaDias");

            migrationBuilder.DropTable(
                name: "Listas");

            migrationBuilder.DropTable(
                name: "Obras");

            migrationBuilder.DropTable(
                name: "Ingredientes");

            migrationBuilder.DropTable(
                name: "Recetas");

            migrationBuilder.DropTable(
                name: "Semanas");

            migrationBuilder.DropTable(
                name: "Gastos");

            migrationBuilder.DropTable(
                name: "Vinetas");
        }
    }
}
