using System.Collections;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Love.Data;

/// <summary>
/// Copia todas las tablas de un contexto a otro con los mismos Id (SQLite → PostgreSQL al migrar,
/// PostgreSQL → SQLite para la copia de seguridad). El destino debe estar vacío y con el esquema creado.
/// </summary>
public static class CopiaDatos
{
    public static async Task<Dictionary<string, int>> Copiar(AppDb origen, AppDb destino, CancellationToken ct = default)
    {
        origen.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        destino.ChangeTracker.AutoDetectChangesEnabled = false;
        var res = new Dictionary<string, int>();
        await using var tx = await destino.Database.BeginTransactionAsync(ct);
        foreach (var t in Ordenadas(destino.Model))
        {
            var set = (IEnumerable)typeof(DbContext).GetMethod(nameof(DbContext.Set), Type.EmptyTypes)!
                .MakeGenericMethod(t.ClrType).Invoke(origen, null)!;
            var filas = new List<object>();
            foreach (var f in set) filas.Add(f);
            foreach (var f in filas) destino.Add(f);
            await destino.SaveChangesAsync(ct);
            destino.ChangeTracker.Clear();
            res[t.GetTableName()!] = filas.Count;
        }
        if (destino.Database.IsNpgsql()) await AjustarSecuencias(destino, ct);
        await tx.CommitAsync(ct);
        return res;
    }

    /// <summary>Las identidades de PostgreSQL no se enteran de los Id insertados a mano: se ponen tras el máximo.</summary>
    public static async Task AjustarSecuencias(AppDb db, CancellationToken ct = default)
    {
        foreach (var t in db.Model.GetEntityTypes())
        {
            var pk = t.FindPrimaryKey();
            if (pk is not { Properties.Count: 1 } || pk.Properties[0].ClrType != typeof(int)) continue;
            var tabla = t.GetTableName()!; var col = pk.Properties[0].GetColumnName();
            await db.Database.ExecuteSqlRawAsync(
                $"SELECT setval(s, COALESCE((SELECT MAX(\"{col}\") FROM \"{tabla}\"), 0) + 1, false) " +
                $"FROM pg_get_serial_sequence('\"{tabla}\"', '{col}') AS s WHERE s IS NOT NULL", ct);
        }
    }

    /// <summary>Tablas en orden de dependencias: primero las que no apuntan a nadie.</summary>
    static List<IEntityType> Ordenadas(IModel m)
    {
        var pendientes = m.GetEntityTypes().Where(t => !t.IsOwned() && t.GetTableName() != null).ToList();
        var hechas = new List<IEntityType>();
        while (pendientes.Count > 0)
        {
            var lista = pendientes.Where(t => t.GetForeignKeys().All(fk => fk.PrincipalEntityType == t || hechas.Contains(fk.PrincipalEntityType))).ToList();
            if (lista.Count == 0) throw new InvalidOperationException("Dependencias circulares entre tablas: " + string.Join(", ", pendientes.Select(p => p.GetTableName())));
            hechas.AddRange(lista); pendientes.RemoveAll(lista.Contains);
        }
        return hechas;
    }

    /// <summary>¿Está vacía la base? (sin usuarios ni mensajes: recién creada).</summary>
    public static async Task<bool> Vacia(AppDb db) => !await db.Usuarios.AnyAsync() && !await db.Mensajes.AnyAsync();
}
