namespace ErpPikis.Service.Common;

public static class RangoFechas
{
    public static DateOnly HoyLocal() => DateOnly.FromDateTime(DateTime.Now);

    public static (DateTime DesdeUtc, DateTime HastaUtc) DiasLocalesEnUtc(DateOnly desde, DateOnly hasta)
    {
        var inicio = desde.ToDateTime(TimeOnly.MinValue, DateTimeKind.Local).ToUniversalTime();
        var fin = hasta.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Local).ToUniversalTime();
        return (inicio, fin);
    }

    public static DateOnly DiaLocal(DateTime fechaUtc) =>
        DateOnly.FromDateTime(DateTime.SpecifyKind(fechaUtc, DateTimeKind.Utc).ToLocalTime());
}
