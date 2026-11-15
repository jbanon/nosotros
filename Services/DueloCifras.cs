using Love.Data;
using Love.Models;
using Microsoft.EntityFrameworkCore;

namespace Love.Services;

/// <summary>
/// Duelo de Cifras entre los dos, en memoria (una sola sala). El servidor reparte los números, cronometra y
/// decide: gana quien se queda más cerca; a igualdad, quien llegó antes. Clavarlo acaba la ronda para los dos.
/// Las rondas terminadas se guardan en <see cref="DueloRonda"/> para el marcador histórico.
/// </summary>
public class DueloCifras(IServiceScopeFactory scopes)
{
    public const int Seg = 45;
    static readonly TimeSpan Cuenta = TimeSpan.FromSeconds(4), Caduca = TimeSpan.FromHours(3);
    public record Jugador(int Id, string Nombre);

    class Res { public int? Valor; public double? Seg; }
    class Ronda
    {
        public int N; public int[] Nums = []; public int Objetivo; public DateTime Inicio;
        public Dictionary<int, Res> Res = new(); public bool Fin; public int? Ganador;
    }
    class Sala
    {
        public Jugador A = null!; public Jugador? B; public DateTime Toque = DateTime.UtcNow;
        public Ronda? Ronda; public Dictionary<int, int> Victorias = new(); public int Empates;
        public Dictionary<int, DateTime> Visto = new();
    }

    readonly Lock l = new();
    Sala? sala;
    (string nombre, DateTime cuando)? salida;
    Dictionary<int, int>? historico; int historicoEmpates;

    Sala? Viva()
    {
        if (sala != null && DateTime.UtcNow - sala.Toque > Caduca) sala = null;
        return sala;
    }
    bool Dentro(Sala s, int id) => s.A.Id == id || s.B?.Id == id;

    /// <summary>Nombre de quien te reta, si hay un reto pendiente para ti.</summary>
    public string? RetoPara(int id)
    {
        lock (l) { var s = Viva(); return s != null && s.B == null && s.A.Id != id ? s.A.Nombre : null; }
    }

    public void Retar(Jugador yo)
    {
        lock (l)
        {
            var s = Viva();
            if (s != null && Dentro(s, yo.Id)) return;
            if (s != null && s.B == null) { Unir(s, yo); return; }   // los dos retan a la vez: se juega
            sala = new Sala { A = yo }; salida = null;
        }
    }

    public void Aceptar(Jugador yo)
    {
        lock (l) { var s = Viva(); if (s != null && s.B == null && s.A.Id != yo.Id) Unir(s, yo); }
    }

    void Unir(Sala s, Jugador yo) { s.B = yo; salida = null; NuevaRonda(s); }

    public void Nueva(Jugador yo)
    {
        lock (l)
        {
            var s = Viva();
            if (s?.B != null && Dentro(s, yo.Id) && (s.Ronda == null || Revisar(s))) NuevaRonda(s);
        }
    }

    public void Salir(Jugador yo)
    {
        lock (l)
        {
            var s = Viva();
            if (s != null && Dentro(s, yo.Id)) { if (s.B != null) salida = (yo.Nombre, DateTime.UtcNow); sala = null; }
        }
    }

    public void Jugada(Jugador yo, int valor)
    {
        lock (l)
        {
            var s = Viva(); var r = s?.Ronda;
            if (s == null || r == null || !Dentro(s, yo.Id) || Revisar(s) || DateTime.UtcNow < r.Inicio || valor <= 0) return;
            var res = r.Res[yo.Id];
            if (res.Valor == null || Math.Abs(valor - r.Objetivo) < Math.Abs(res.Valor.Value - r.Objetivo))
            {
                res.Valor = valor; res.Seg = Math.Round((DateTime.UtcNow - r.Inicio).TotalSeconds, 1);
            }
            s.Toque = DateTime.UtcNow;
            Revisar(s);
        }
    }

    void NuevaRonda(Sala s)
    {
        var rnd = Random.Shared;
        var bolsa = Enumerable.Range(1, 10).Concat(Enumerable.Range(1, 10)).Concat([25, 50, 75, 100]).ToList();
        var nums = new int[6];
        for (int i = 0; i < 6; i++) { var k = rnd.Next(bolsa.Count); nums[i] = bolsa[k]; bolsa.RemoveAt(k); }
        s.Ronda = new Ronda
        {
            N = (s.Ronda?.N ?? 0) + 1, Nums = nums, Objetivo = rnd.Next(101, 1000), Inicio = DateTime.UtcNow + Cuenta,
            Res = new() { [s.A.Id] = new Res(), [s.B!.Id] = new Res() },
        };
        s.Toque = DateTime.UtcNow;
    }

    static int? Dist(Res r, int obj) => r.Valor is int v ? Math.Abs(v - obj) : null;

    /// <summary>Cierra la ronda si toca (tiempo agotado o alguien lo ha clavado). Devuelve si está terminada.</summary>
    bool Revisar(Sala s)
    {
        var r = s.Ronda;
        if (r == null) return true;
        if (r.Fin) return true;
        var clavado = r.Res.Values.Any(x => Dist(x, r.Objetivo) == 0);
        if (!clavado && DateTime.UtcNow < r.Inicio.AddSeconds(Seg + 1)) return false;
        r.Fin = true;
        var (a, b) = (r.Res[s.A.Id], r.Res[s.B!.Id]);
        int da = Dist(a, r.Objetivo) ?? int.MaxValue, db = Dist(b, r.Objetivo) ?? int.MaxValue;
        r.Ganador = da < db ? s.A.Id : db < da ? s.B.Id
                  : da == int.MaxValue ? null : a.Seg < b.Seg ? s.A.Id : b.Seg < a.Seg ? s.B.Id : null;
        if (r.Ganador is int g) s.Victorias[g] = s.Victorias.GetValueOrDefault(g) + 1; else s.Empates++;
        var fila = new DueloRonda
        {
            Numeros = string.Join(",", r.Nums), Objetivo = r.Objetivo, AId = s.A.Id, AValor = a.Valor, ASeg = a.Seg,
            BId = s.B.Id, BValor = b.Valor, BSeg = b.Seg, GanadorId = r.Ganador,
        };
        if (historico != null) { if (r.Ganador is int h) historico[h] = historico.GetValueOrDefault(h) + 1; else historicoEmpates++; }
        _ = Task.Run(async () =>
        {
            using var sc = scopes.CreateScope();
            var db = sc.ServiceProvider.GetRequiredService<AppDb>();
            db.DueloRondas.Add(fila); await db.SaveChangesAsync();
        });
        return true;
    }

    async Task CargarHistorico()
    {
        if (historico != null) return;
        using var sc = scopes.CreateScope();
        var db = sc.ServiceProvider.GetRequiredService<AppDb>();
        var g = await db.DueloRondas.GroupBy(x => x.GanadorId).Select(x => new { x.Key, N = x.Count() }).ToListAsync();
        lock (l)
        {
            if (historico != null) return;
            historico = g.Where(x => x.Key != null).ToDictionary(x => x.Key!.Value, x => x.N);
            historicoEmpates = g.Where(x => x.Key == null).Sum(x => x.N);
        }
    }

    public async Task<object> Estado(Jugador yo)
    {
        await CargarHistorico();
        lock (l)
        {
            var s = Viva();
            var ahora = DateTime.UtcNow;
            object Hist(int rivalId) => new { yo = historico!.GetValueOrDefault(yo.Id), rival = historico!.GetValueOrDefault(rivalId), empates = historicoEmpates };
            if (s == null || !Dentro(s, yo.Id))
            {
                var aviso = salida is { } x && ahora - x.cuando < TimeSpan.FromMinutes(2) && x.nombre != yo.Nombre ? $"{x.nombre} ha salido del duelo" : null;
                return s != null && s.B == null
                    ? new { estado = "reto", rival = new { nombre = s.A.Nombre }, aviso }
                    : (object)new { estado = "nada", aviso };
            }
            s.Visto[yo.Id] = ahora;
            if (s.B == null) return new { estado = "esperando" };
            var riv = s.A.Id == yo.Id ? s.B : s.A;
            var r = s.Ronda!;
            var fin = Revisar(s);
            var (mio, suyo) = (r.Res[yo.Id], r.Res[riv.Id]);
            return new
            {
                estado = fin ? "fin" : "ronda",
                rival = new { nombre = riv.Nombre, conectado = s.Visto.TryGetValue(riv.Id, out var v) && ahora - v < TimeSpan.FromSeconds(8) },
                sesion = new { yo = s.Victorias.GetValueOrDefault(yo.Id), rival = s.Victorias.GetValueOrDefault(riv.Id), empates = s.Empates },
                historico = Hist(riv.Id),
                ronda = new
                {
                    n = r.N, nums = r.Nums, objetivo = r.Objetivo,
                    msParaInicio = (int)(r.Inicio - ahora).TotalMilliseconds,
                    yo = new { valor = mio.Valor, d = Dist(mio, r.Objetivo), seg = mio.Seg },
                    // durante la ronda solo se ve lo cerca que va el otro, no su número
                    rival = new { valor = fin ? suyo.Valor : null, d = Dist(suyo, r.Objetivo), seg = suyo.Seg },
                    ganador = !fin ? null : r.Ganador == null ? "empate" : r.Ganador == yo.Id ? "yo" : "rival",
                },
            };
        }
    }
}
