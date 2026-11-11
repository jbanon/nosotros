using System.Text.Json;
using Love.Data;
using Love.Models;
using Love.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Love.Pages.Admin.Adviento;

public class EditarModel(AdvientoService adv, AppDb db, FileStorage files) : PageModel
{
    [BindProperty] public AdvientoCasilla Casilla { get; set; } = new();
    [BindProperty] public IFormFile? Foto { get; set; }
    [BindProperty] public bool QuitarFoto { get; set; }
    [BindProperty] public bool QuitarVideo { get; set; }
    static readonly HashSet<string> ExtVideo = [".mp4", ".m4v", ".mov", ".webm"];
    /// <summary>Tope del vídeo: 1 GB (un villancico de 3 min en 1080p ronda los 300 MB).</summary>
    public const long MaxVideo = 1L << 30;
    public DateOnly Hoy => Reloj.Hoy;
    public List<Vineta> Vinetas { get; set; } = new();

    async Task<AdvientoCasilla?> Buscar(int dia)
    {
        var anio = AdvientoService.AnioDe(Reloj.Hoy);
        return await adv.Casilla(anio, dia) ?? await adv.Casilla(Reloj.Hoy.Year, dia);
    }

    public async Task<IActionResult> OnGet(int dia)
    {
        var c = await Buscar(dia);
        if (c == null) return NotFound();
        Casilla = c;
        Vinetas = await db.Vinetas.Where(v => v.Activa).OrderBy(v => v.Libro).ThenBy(v => v.FraseAutor).ToListAsync();
        return Page();
    }

    public async Task<IActionResult> OnPost(int dia)
    {
        var c = await Buscar(dia);
        if (c == null) return NotFound();
        if (!string.IsNullOrWhiteSpace(Casilla.Datos))
        {
            try { JsonDocument.Parse(Casilla.Datos); }
            catch (JsonException e) { ModelState.AddModelError("Casilla.Datos", "El JSON no es válido: " + e.Message); Casilla.Id = c.Id; Casilla.Anio = c.Anio; Casilla.Dia = c.Dia; Casilla.FotoPath = c.FotoPath; return Page(); }
        }
        c.Titulo = Casilla.Titulo.Trim();
        c.Intro = string.IsNullOrWhiteSpace(Casilla.Intro) ? null : Casilla.Intro.Trim();
        c.Tipo = Casilla.Tipo;
        c.Letra = string.IsNullOrWhiteSpace(Casilla.Letra) ? null : Casilla.Letra.Trim().ToUpperInvariant()[..1];
        c.Pregunta = string.IsNullOrWhiteSpace(Casilla.Pregunta) ? null : Casilla.Pregunta.Trim();
        c.Respuesta = string.IsNullOrWhiteSpace(Casilla.Respuesta) ? null : Casilla.Respuesta.Trim();
        c.Datos = string.IsNullOrWhiteSpace(Casilla.Datos) ? "{}" : Casilla.Datos.Trim();
        c.VinetaId = Casilla.VinetaId;
        if (QuitarFoto) { files.Borrar(c.FotoPath); c.FotoPath = null; }
        if (QuitarVideo) { files.Borrar(c.VideoPath); c.VideoPath = null; }
        if (Foto != null && Foto.Length > 0)
        {
            files.Borrar(c.FotoPath);
            (c.FotoPath, _) = await files.Guardar(Foto, "adviento");
        }
        await db.SaveChangesAsync();
        return Redirect("/Admin/Adviento");
    }

    /// <summary>
    /// Subida del vídeo por trozos de 8 MB: cada petición cabe en el límite de nginx (30 MB) y de Kestrel,
    /// y si se corta la conexión se reintenta solo el trozo. El último trozo coloca el vídeo en la casilla.
    /// </summary>
    public async Task<IActionResult> OnPostTrozo(int dia, string subida, long desde, long total, string nombre, IFormFile? trozo)
    {
        var c = await Buscar(dia);
        if (c == null) return NotFound();
        var ext = Path.GetExtension(nombre ?? "").ToLowerInvariant();
        if (!Guid.TryParseExact(subida, "N", out _) || trozo == null || desde < 0) return BadRequest(new { error = "Petición no válida" });
        if (!ExtVideo.Contains(ext)) return BadRequest(new { error = "Formato no admitido: sube un .mp4, .mov o .webm" });
        if (total <= 0 || total > MaxVideo) return BadRequest(new { error = "El vídeo pasa de 1 GB" });

        var dirTmp = Path.Combine(files.Raiz, "tmp");
        Directory.CreateDirectory(dirTmp);
        if (desde == 0)
            foreach (var viejo in new DirectoryInfo(dirTmp).GetFiles("*.part").Where(f => f.LastWriteTimeUtc < DateTime.UtcNow.AddDays(-1)))
                try { viejo.Delete(); } catch { }
        var tmp = Path.Combine(dirTmp, subida + ".part");
        long largo;
        await using (var fs = new FileStream(tmp, FileMode.OpenOrCreate, FileAccess.Write))
        {
            // Un trozo repetido (reintento) se reescribe; si falta alguno anterior, se pide desde donde va.
            if (fs.Length < desde) return new ConflictObjectResult(new { recibido = fs.Length });
            fs.SetLength(desde);
            fs.Position = desde;
            await trozo.CopyToAsync(fs);
            largo = fs.Length;
        }
        if (largo > total) { System.IO.File.Delete(tmp); return BadRequest(new { error = "El vídeo llegó con un tamaño distinto; vuelve a subirlo" }); }
        if (largo < total) return new JsonResult(new { recibido = largo });

        var rel = $"adviento/{Guid.NewGuid():N}{ext}";
        Directory.CreateDirectory(Path.Combine(files.Raiz, "adviento"));
        System.IO.File.Move(tmp, files.RutaAbsoluta(rel));
        files.Borrar(c.VideoPath);
        c.VideoPath = rel;
        await db.SaveChangesAsync();
        return new JsonResult(new { recibido = largo, listo = true, url = $"/Adviento/Video/{c.Id}?v={Path.GetFileNameWithoutExtension(rel)[..8]}" });
    }

    public async Task<IActionResult> OnPostReiniciar(int dia)
    {
        var c = await Buscar(dia);
        if (c == null) return NotFound();
        c.AbiertaEl = null; c.ResueltaEl = null; c.CanjeadoEl = null; c.Estado = null; c.Pistas = 0;
        await db.SaveChangesAsync();
        return Redirect($"/Admin/Adviento/Editar/{dia}");
    }
}
