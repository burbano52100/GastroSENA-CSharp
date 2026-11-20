using System.Security.Claims;
using GastroSENA.Api.Data;
using GastroSENA.Api.Dtos;
using GastroSENA.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace GastroSENA.Api.Endpoints;

public static class UsuariosEndpoints
{
    public static void MapUsuariosEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/usuarios").RequireAuthorization();

        // GET /api/usuarios/me -> perfil, saldo y fichas
        grupo.MapGet("/me", async (ClaimsPrincipal user, GastroSenaDbContext db) =>
        {
            var usuario = await db.Usuarios.FindAsync(user.UsuarioActual());
            return usuario is null ? Results.NotFound() : Results.Ok(Mapeos.ToUsuarioDto(usuario));
        });

        // POST /api/usuarios/me/recargas -> suma saldo (simulado, sin pasarela de pago)
        grupo.MapPost("/me/recargas", async (RecargaRequest req, ClaimsPrincipal user, GastroSenaDbContext db) =>
        {
            if (req.Monto <= 0 || req.Monto > 1000000m)
                return Results.BadRequest(new { mensaje = "El monto debe estar entre 1 y 1000000." });

            var usuario = await db.Usuarios.FindAsync(user.UsuarioActual());
            if (usuario is null) return Results.NotFound();

            usuario.Saldo += req.Monto;
            db.Transacciones.Add(new Transaccion
            {
                UsuarioId = usuario.Id,
                Tipo = TipoTransaccion.Recarga,
                Monto = req.Monto,
                Descripcion = "Recarga de saldo"
            });
            await db.SaveChangesAsync();
            return Results.Ok(Mapeos.ToUsuarioDto(usuario));
        });

        // GET /api/usuarios/me/movimientos -> últimos 100 movimientos de saldo y fichas
        grupo.MapGet("/me/movimientos", async (ClaimsPrincipal user, GastroSenaDbContext db) =>
        {
            var uid = user.UsuarioActual();
            var movimientos = await db.Transacciones
                .Where(t => t.UsuarioId == uid)
                .OrderByDescending(t => t.FechaHora)
                .Take(100)
                .ToListAsync();
            return Results.Ok(movimientos.Select(t => Mapeos.ToTransaccionDto(t)).ToList());
        });
    }
}
