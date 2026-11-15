using Love.Data;
using Love.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Love.Services;

/// <summary>
/// Crucigrama compartido: los dos rellenan la misma rejilla. Estado en memoria con escritura en
/// <see cref="CrucigramaPartida"/> a cada cambio; dónde está cada uno (cursor) solo en memoria.
/// </summary>
public class CruciCompartido(IServiceScopeFactory scopes, CrucigramaGenerador gen)
{
    class Est
    {
        public Dictionary<string, (string l, int quien)> Letras = new();
        public HashSet<string> Ayudas = new();
        public bool Rendido; public DateTime? ResueltoEl; public int V;
    }
    record Cursor(string? K, string? Dir, DateTime Cuando);
    record Guardado(Dictionary<string, object[]> letras, List<string> ayudas, bool rendido);

    readonly SemaphoreSlim l = new(1, 1);
    readonly Dictionary<int, Est> cache = new();
    readonly Dictionary<(int semilla, int quien), Cursor> cursores = new();

    async Task<Est> Cargar(AppDb db, int semilla)
    {
        if (cache.TryGetValue(semilla, out var e)) return e;
        e = new Est();
        var fila = await db.CrucigramaPartidas.AsNoTracking().FirstOrDefaultAsync(p => p.Semilla == semilla);
        if (fila != null)
        {
            var g = JsonSerializer.Deserialize<Guardado>(fila.Estado);
            if (g?.letras != null)
                foreach (var (k, v) in g.letras) e.Letras[k] = (((JsonElement)v[0]).GetString()!, ((JsonElement)v[1]).GetInt32());
            if (g?.ayudas != null) e.Ayudas = g.ayudas.ToHashSet();
            e.Rendido = g?.rendido ?? false; e.ResueltoEl = fila.ResueltoEl;
        }
        return cache[semilla] = e;
    }

    async Task Guardar(AppDb db, int semilla, Est e)
    {
        var json = JsonSerializer.Serialize(new Guardado(e.Letras.ToDictionary(x => x.Key, x => new object[] { x.Value.l, x.Value.quien }), e.Ayudas.ToList(), e.Rendido));
        var fila = await db.CrucigramaPartidas.FirstOrDefaultAsync(p => p.Semilla == semilla);
        if (fila == null) db.CrucigramaPartidas.Add(fila = new CrucigramaPartida { Semilla = semilla });
        fila.Estado = json; fila.ResueltoEl = e.ResueltoEl; fila.Actualizado = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    /// <summary>Aplica cambios (letra, borrar, ayuda, rendirse) y devuelve el estado. tipo: "l" letra, "a" ayuda.</summary>
    public async Task<object> Operar(int semilla, int yo, string? k, string? dir, IEnumerable<(string k, string l)> letras, bool ayuda, bool rendirse)
    {
        await l.WaitAsync();
        try
        {
            using var sc = scopes.CreateScope();
            var db = sc.ServiceProvider.GetRequiredService<AppDb>();
            var e = await Cargar(db, semilla);
            var sol = gen.Solucion(semilla);
            cursores[(semilla, yo)] = new Cursor(k, dir, DateTime.UtcNow);
            bool cambio = false;
            if (e.ResueltoEl == null)
            {
                foreach (var (ck, cl) in letras)
                {
                    if (!sol.ContainsKey(ck)) continue;
                    var letra = Criptofrase.SoloLetras(cl);
                    if (letra.Length > 1) letra = letra[^1..];
                    if (letra == "") cambio |= e.Letras.Remove(ck);
                    else if (!e.Letras.TryGetValue(ck, out var v) || v.l != letra) { e.Letras[ck] = (letra, yo); cambio = true; }
                }
                if (ayuda && k != null && sol.TryGetValue(k, out var bien)) { e.Letras[k] = (bien, yo); e.Ayudas.Add(k); cambio = true; }
                if (rendirse) { foreach (var (sk, sl) in sol) if (!e.Letras.TryGetValue(sk, out var v) || v.l != sl) e.Letras[sk] = (sl, yo); e.Rendido = true; cambio = true; }
                if (cambio && sol.All(x => e.Letras.TryGetValue(x.Key, out var v) && v.l == x.Value)) e.ResueltoEl = DateTime.UtcNow;
            }
            if (cambio) { e.V++; await Guardar(db, semilla, e); }

            var otro = cursores.Where(x => x.Key.semilla == semilla && x.Key.quien != yo && DateTime.UtcNow - x.Value.Cuando < TimeSpan.FromSeconds(12))
                .Select(x => new { id = x.Key.quien, x.Value.K, x.Value.Dir }).FirstOrDefault();
            string? nombreOtro = otro == null ? null : await db.Usuarios.Where(u => u.Id == otro.id).Select(u => u.Nombre).FirstOrDefaultAsync();
            return new
            {
                v = e.V,
                letras = e.Letras.ToDictionary(x => x.Key, x => new object[] { x.Value.l, x.Value.quien == yo ? 1 : 0 }),
                ayudas = e.Ayudas,
                resuelto = e.ResueltoEl != null,
                rendido = e.Rendido,
                otro = otro == null ? null : new { nombre = nombreOtro, k = otro.K, dir = otro.Dir },
            };
        }
        finally { l.Release(); }
    }
}
