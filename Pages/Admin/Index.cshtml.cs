using Love.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Love.Pages.Admin;

public class IndexModel(AppDb db) : PageModel
{
    public int NumMensajes { get; set; }
    public int NumFechas { get; set; }
    public int RespuestasNuevas { get; set; }
    public int VinetasSinFrase { get; set; }
    public async Task OnGet()
    {
        RespuestasNuevas = await db.Vinetas.CountAsync(v => v.RespuestaRaquel != null && !v.RespuestaVista);
        VinetasSinFrase = await db.Vinetas.CountAsync(v => v.Activa && string.IsNullOrEmpty(v.FraseMia));
        NumMensajes = await db.Mensajes.CountAsync(m => m.Activo);
        NumFechas = await db.FechasEspeciales.CountAsync(f => f.Activo);
    }
}
