using Love.Data;
using Love.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Love.Pages.Adviento;

/// <summary>Sirve el vídeo de una casilla con rangos (para poder adelantar y empezar antes de bajarlo entero).</summary>
public class VideoModel(AppDb db, FileStorage files) : PageModel
{
    public async Task<IActionResult> OnGet(int id)
    {
        var c = await db.Adviento.FindAsync(id);
        if (c?.VideoPath == null) return NotFound();
        if (!User.EsAdmin() && !AdvientoService.Abierta(c, Reloj.Hoy)) return NotFound();
        var p = files.RutaAbsoluta(c.VideoPath);
        if (!System.IO.File.Exists(p)) return NotFound();
        Response.Headers.CacheControl = "private, max-age=604800";
        return new PhysicalFileResult(p, MimeVideo(p)) { EnableRangeProcessing = true };
    }

    /// <summary>Los .mov de móvil son MP4 por dentro: con video/mp4 Chrome en Android los reproduce sin problema.</summary>
    public static string MimeVideo(string ruta) => Path.GetExtension(ruta).ToLowerInvariant() == ".webm" ? "video/webm" : "video/mp4";
}
