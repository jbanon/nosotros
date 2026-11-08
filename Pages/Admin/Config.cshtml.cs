using System.Globalization;
using System.IO.Compression;
using Microsoft.Data.Sqlite;
using Love.Models;
using Love.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Love.Pages.Admin;

public class ConfigModel(ConfigService config, PinAuth auth, FileStorage files, SeedService seed) : PageModel
{
    [BindProperty] public string Objetivo { get; set; } = "";
    [BindProperty] public string Veva { get; set; } = "";
    [BindProperty] public string Hipoteca { get; set; } = "";
    [BindProperty] public string Firma { get; set; } = "";
    [BindProperty] public string NombreElla { get; set; } = "";
    [BindProperty] public int RacionesLista { get; set; } = 12;
    public string? Mensaje { get; set; }

    public async Task OnGet() => await Cargar();

    async Task Cargar()
    {
        Objetivo = (await config.GetDecimal(ConfigClaves.ObjetivoCompra, 350)).ToString("0.##", CultureInfo.InvariantCulture);
        Veva = (await config.GetDecimal(ConfigClaves.ImporteVeva, 300)).ToString("0.##", CultureInfo.InvariantCulture);
        Hipoteca = (await config.GetDecimal(ConfigClaves.ImporteHipoteca, 900)).ToString("0.##", CultureInfo.InvariantCulture);
        Firma = await config.Get(ConfigClaves.Firma, "Javier");
        NombreElla = await config.Get(ConfigClaves.NombreElla, "Raquel");
        RacionesLista = (int)await config.GetDecimal(ConfigClaves.RacionesLista, 12);
    }

    static decimal D(string s, decimal def) => decimal.TryParse(s.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : def;

    public async Task<IActionResult> OnPost()
    {
        await config.SetDecimal(ConfigClaves.ObjetivoCompra, D(Objetivo, 350));
        await config.SetDecimal(ConfigClaves.ImporteVeva, D(Veva, 300));
        await config.SetDecimal(ConfigClaves.ImporteHipoteca, D(Hipoteca, 900));
        await config.Set(ConfigClaves.Firma, Firma.Trim());
        await config.Set(ConfigClaves.NombreElla, NombreElla.Trim());
        await config.Set(ConfigClaves.RacionesLista, Math.Clamp(RacionesLista, 1, 50).ToString());
        Mensaje = "Guardado";
        await Cargar();
        return Page();
    }

    public async Task<IActionResult> OnPostPin(string rol, string pin1, string pin2)
    {
        await Cargar();
        if (pin1 != pin2 || pin1.Length < 4 || !pin1.All(char.IsDigit)) { Mensaje = "Los PIN no coinciden o son demasiado cortos"; return Page(); }
        await auth.CambiarPin(Enum.Parse<Rol>(rol), pin1);
        Mensaje = $"PIN de {rol} cambiado";
        return Page();
    }

    public async Task<IActionResult> OnPostImportar(IFormFile recetas)
    {
        await Cargar();
        using var sr = new StreamReader(recetas.OpenReadStream());
        try { var n = await seed.CargarRecetas(await sr.ReadToEndAsync()); Mensaje = $"{n} recetas importadas"; }
        catch (Exception e) { Mensaje = "No se pudo importar: " + e.Message; }
        return Page();
    }

    public IActionResult OnGetBackup()
    {
        var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            if (System.IO.File.Exists(files.RutaDb))
            {
                // Copia consistente aunque SQLite esté en modo WAL
                var tmp = Path.Combine(Path.GetTempPath(), $"love-backup-{Guid.NewGuid():N}.db");
                using (var src = new SqliteConnection($"Data Source={files.RutaDb}"))
                using (var dst = new SqliteConnection($"Data Source={tmp}"))
                {
                    src.Open(); dst.Open(); src.BackupDatabase(dst);
                }
                SqliteConnection.ClearAllPools();
                zip.CreateEntryFromFile(tmp, "love.db");
                System.IO.File.Delete(tmp);
            }
            if (Directory.Exists(files.Raiz))
                foreach (var f in Directory.EnumerateFiles(files.Raiz, "*", SearchOption.AllDirectories))
                    zip.CreateEntryFromFile(f, "adjuntos/" + Path.GetRelativePath(files.Raiz, f).Replace('\\', '/'));
        }
        ms.Position = 0;
        return File(ms, "application/zip", $"nosotros-{Reloj.Hoy:yyyyMMdd}.zip");
    }
}
