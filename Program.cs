using System.Globalization;
using Love.Data;
using Love.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false);

var cultura = new CultureInfo("es-ES");
CultureInfo.DefaultThreadCurrentCulture = cultura;
CultureInfo.DefaultThreadCurrentUICulture = cultura;

builder.Services.AddRazorPages(o =>
{
    o.Conventions.AuthorizeFolder("/");
    o.Conventions.AllowAnonymousToPage("/Login");
    o.Conventions.AuthorizeFolder("/Admin", "Admin");
    o.Conventions.AuthorizeFolder("/Gastos", "Admin");
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(o =>
{
    o.LoginPath = "/Login";
    o.AccessDeniedPath = "/";
    o.ExpireTimeSpan = TimeSpan.FromDays(90);
    o.SlidingExpiration = true;
    o.Cookie.Name = "love.sesion";
    o.Cookie.HttpOnly = true;
    o.Cookie.SameSite = SameSiteMode.Lax;
});
builder.Services.AddAuthorizationBuilder().AddPolicy("Admin", p => p.RequireRole("Admin"));

var fs = new FileStorage(builder.Environment, builder.Configuration);
Directory.CreateDirectory(Path.GetDirectoryName(fs.RutaDb)!);
Directory.CreateDirectory(fs.Raiz);
builder.Services.AddSingleton(fs);
builder.Services.AddDbContext<AppDb>(o => o.UseSqlite($"Data Source={fs.RutaDb}"));

builder.Services.AddScoped<ConfigService>();
builder.Services.AddScoped<MensajeService>();
builder.Services.AddScoped<ListaCompraService>();
builder.Services.AddScoped<GastoService>();
builder.Services.AddScoped<PinAuth>();
builder.Services.AddScoped<SeedService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
    await scope.ServiceProvider.GetRequiredService<SeedService>().Ejecutar();

if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler("/Login");

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(cultura),
    SupportedCultures = [cultura],
    SupportedUICultures = [cultura]
});
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();

app.Run();
