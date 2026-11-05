using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Love.Services;

namespace Love.Pages;

public class IndexModel : PageModel
{
    public IActionResult OnGet() => Redirect(User.EsAdmin() ? "/Gastos" : "/Amor");
}
