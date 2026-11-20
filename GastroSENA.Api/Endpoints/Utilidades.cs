using System.Security.Claims;

namespace GastroSENA.Api.Endpoints;

public static class Utilidades
{
    private static readonly TimeZoneInfo Zona = TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");

    /// <summary>Id del usuario autenticado (claim NameIdentifier del token).</summary>
    public static int UsuarioActual(this ClaimsPrincipal user) =>
        int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());

    /// <summary>Fecha de hoy en Colombia. Reservas y control de acceso usan esta fecha.</summary>
    public static DateOnly HoyEnColombia() =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zona));

    /// <summary>Inicio de un día (hora de Colombia) expresado en UTC.</summary>
    public static DateTime InicioDiaUtc(DateOnly dia) =>
        TimeZoneInfo.ConvertTimeToUtc(dia.ToDateTime(TimeOnly.MinValue), Zona);
}
