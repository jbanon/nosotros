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
// Base de datos: PostgreSQL. La cadena va fuera del repo (/etc/love.env o appsettings.Local.json).
// Si App_Data/love.db (la base SQLite de antes) existe y Postgres está vacío, SeedService la importa al arrancar.
var cadena = builder.Configuration.GetConnectionString("Love");
if (string.IsNullOrWhiteSpace(cadena))
    throw new InvalidOperationException("Falta la cadena de PostgreSQL: ConnectionStrings__Love=Host=localhost;Database=love;Username=love;Password=... " +
                                        "en /etc/love.env (sudo ./deploy/instalar-postgres.sh la crea) o ConnectionStrings:Love en appsettings.Local.json.");
// DateTime se guarda tal cual (timestamp sin zona), como hacía SQLite: los valores ya son UTC donde toca.
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
builder.Services.AddDbContext<AppDb>(o => o.UseNpgsql(cadena));

builder.Services.AddScoped<ConfigService>();
builder.Services.AddScoped<MensajeService>();
builder.Services.AddScoped<ListaCompraService>();
builder.Services.AddScoped<ZonaAuto>();
builder.Services.AddScoped<GastoService>();
builder.Services.AddScoped<SugerenciaService>();
builder.Services.AddScoped<RecurrenteService>();
builder.Services.AddScoped<PinAuth>();
builder.Services.AddScoped<SeedService>();
builder.Services.AddScoped<AdvientoService>();
builder.Services.AddScoped<VinetaService>();
builder.Services.AddScoped<ArteService>();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<AgenteService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<AgenteService>());
builder.Services.AddAntiforgery(o => o.HeaderName = "RequestVerificationToken");

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
