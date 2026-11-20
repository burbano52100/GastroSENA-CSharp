using System.Security.Claims;
using GastroSENA.Api.Data;
using GastroSENA.Api.Dtos;
using GastroSENA.Api.Models;
using Microsoft.AspNetCore.Authorization;

namespace GastroSENA.Api.Endpoints;

public static class FichasEndpoints
{
    public static void MapFichasEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/fichas").RequireAuthorization();

        // GET /api/fichas/precio -> precio de una ficha (configurable)
        grupo.MapGet("/precio", (IConfiguration config) =>
            Results.Ok(new { precioFicha = PrecioFicha(config) }));

        // POST /api/fichas/compras -> compra fichas descontando saldo
        grupo.MapPost("/compras", async (CompraFichasRequest req, ClaimsPrincipal user, GastroSenaDbContext db, IConfiguration config) =>
        {
            if (req.Cantidad is < 1 or > 20)
                return Results.BadRequest(new { mensaje = "Puedes comprar entre 1 y 20 fichas por operación." });

            var usuario = await db.Usuarios.FindAsync(user.UsuarioActual());
            if (usuario is null) return Results.NotFound();

            var costo = req.Cantidad * PrecioFicha(config);
            if (usuario.Saldo < costo)
                return Results.BadRequest(new { mensaje = "Saldo insuficiente.", costo, saldo = usuario.Saldo });

            usuario.Saldo -= costo;
            usuario.Fichas += req.Cantidad;
            db.Transacciones.Add(new Transaccion
            {
                UsuarioId = usuario.Id,
                Tipo = TipoTransaccion.CompraFichas,
                Monto = -costo,
                Fichas = req.Cantidad,
                Descripcion = $"Compra de {req.Cantidad} ficha(s)"
            });
            await db.SaveChangesAsync();
            return Results.Ok(Mapeos.ToUsuarioDto(usuario));
        });
    }

    public static decimal PrecioFicha(IConfiguration config) =>
        config.GetValue<decimal>("GastroSena:PrecioFicha", 3500m);
}
