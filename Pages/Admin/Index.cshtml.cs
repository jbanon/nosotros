using Love.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Love.Pages.Admin;

public class IndexModel(AppDb db) : PageModel
{
    public int NumMensajes { get; set; }
    public int NumFechas { get; set; }
    public async Task OnGet()
    {
        NumMensajes = await db.Mensajes.CountAsync(m => m.Activo);
        NumFechas = await db.FechasEspeciales.CountAsync(f => f.Activo);
    }
}
