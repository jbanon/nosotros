namespace Love.Services;

public static class Reloj
{
    static readonly TimeZoneInfo Tz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid");
    public static DateTime Ahora => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Tz);
    public static DateOnly Hoy => DateOnly.FromDateTime(Ahora);
    /// <summary>Una hora guardada en UTC, en hora de Madrid (no depende de la zona del servidor).</summary>
    public static DateTime Local(DateTime utc) => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Tz);
    public static DateOnly Lunes(DateOnly d) => d.AddDays(-(((int)d.DayOfWeek + 6) % 7));
}
