using Microsoft.EntityFrameworkCore;

namespace Love.Data;

/// <summary>
/// El mismo modelo sobre un fichero SQLite. La app vivió en SQLite hasta noviembre de 2026; ahora va en
/// PostgreSQL y este contexto (con sus migraciones en <c>Data/MigracionesSqlite</c>) solo se usa para importar
/// la base antigua la primera vez y para la copia de seguridad descargable (un love.db portable).
/// </summary>
public class AppDbSqlite(DbContextOptions<AppDbSqlite> o) : AppDb(o)
{
    public static AppDbSqlite Abrir(string ruta) =>
        new(new DbContextOptionsBuilder<AppDbSqlite>().UseSqlite($"Data Source={ruta};Pooling=False").Options);
}

/// <summary>Para <c>dotnet ef ... --context AppDbSqlite</c> (comprobar que sus migraciones siguen al día).</summary>
public class AppDbSqliteDiseno : Microsoft.EntityFrameworkCore.Design.IDesignTimeDbContextFactory<AppDbSqlite>
{
    public AppDbSqlite CreateDbContext(string[] args) => AppDbSqlite.Abrir("App_Data/diseno.db");
}
