using System.Globalization;
using Love.Data;
using Love.Models;
using Microsoft.EntityFrameworkCore;

namespace Love.Services;

public class ConfigService(AppDb db)
{
    public async Task<string> Get(string clave, string porDefecto = "") =>
        (await db.Configs.FindAsync(clave))?.Valor ?? porDefecto;

    public async Task<decimal> GetDecimal(string clave, decimal porDefecto) =>
        decimal.TryParse(await Get(clave), NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : porDefecto;

    public async Task Set(string clave, string valor)
    {
        var c = await db.Configs.FindAsync(clave);
        if (c == null) db.Configs.Add(new Config { Clave = clave, Valor = valor });
        else c.Valor = valor;
        await db.SaveChangesAsync();
    }

    public Task SetDecimal(string clave, decimal v) => Set(clave, v.ToString(CultureInfo.InvariantCulture));
}
