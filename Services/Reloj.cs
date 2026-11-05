namespace Love.Services;

public static class Reloj
{
    static readonly TimeZoneInfo Tz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid");
    public static DateTime Ahora => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Tz);
    public static DateOnly Hoy => DateOnly.FromDateTime(Ahora);
    public static DateOnly Lunes(DateOnly d) => d.AddDays(-(((int)d.DayOfWeek + 6) % 7));
}
