namespace BarbeariaDoZe.Negocio;

public static class HorarioLocal
{
    private static readonly TimeZoneInfo Zona = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
    public static DateTime Agora => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zona);
    public static DateTime Hoje => Agora.Date;
}
