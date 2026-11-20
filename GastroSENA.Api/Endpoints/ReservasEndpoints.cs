using System.Security.Claims;
using GastroSENA.Api.Data;
using GastroSENA.Api.Dtos;
using GastroSENA.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace GastroSENA.Api.Endpoints;

public static class ReservasEndpoints
{
    public static void MapReservasEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/reservas").RequireAuthorization();

        // POST /api/reservas -> reserva un almuerzo para una fecha (consume 1 ficha)
        grupo.MapPost("/", async (ReservaRequest req, ClaimsPrincipal user, GastroSenaDbContext db) =>
        {
            if (req.Fecha < Utilidades.HoyEnColombia())
                return Results.BadRequest(new { mensaje = "No puedes reservar para una fecha pasada." });

            var uid = user.UsuarioActual();
            var usuario = await db.Usuarios.FindAsync(uid);
            if (usuario is null || !usuario.Activo) return Results.NotFound();

            if (usuario.Fichas < 1)
                return Results.BadRequest(new { mensaje = "No tienes fichas disponibles. Compra fichas primero." });

            var yaTiene = await db.Reservas.AnyAsync(r =>
                r.UsuarioId == uid && r.Fecha == req.Fecha && r.Estado != EstadoReserva.Cancelada);
            if (yaTiene)
                return Results.Conflict(new { mensaje = "Ya tienes una reserva para esa fecha." });

            var reserva = new Reserva { UsuarioId = uid, Fecha = req.Fecha };
            usuario.Fichas -= 1;
            db.Reservas.Add(reserva);
            db.Transacciones.Add(new Transaccion
            {
                UsuarioId = uid,
                Tipo = TipoTransaccion.ReservaCreada,
                Fichas = -1,
                Descripcion = $"Reserva para {req.Fecha:yyyy-MM-dd}"
            });
            await db.SaveChangesAsync();

            reserva.Usuario = usuario;
            return Results.Created($"/api/reservas/{reserva.Id}", Mapeos.ToReservaDto(reserva));
        });

        // GET /api/reservas/me -> mis reservas (las más recientes primero)
        grupo.MapGet("/me", async (ClaimsPrincipal user, GastroSenaDbContext db) =>
        {
            var uid = user.UsuarioActual();
            var reservas = await db.Reservas
                .Include(r => r.Usuario)
                .Where(r => r.UsuarioId == uid)
                .OrderByDescending(r => r.Fecha)
                .ToListAsync();
            return Results.Ok(reservas.Select(r => Mapeos.ToReservaDto(r)).ToList());
        });

        // GET /api/reservas/{id} -> detalle (solo el dueño o un admin)
        grupo.MapGet("/{id:int}", async (int id, ClaimsPrincipal user, GastroSenaDbContext db) =>
        {
            var reserva = await db.Reservas.Include(r => r.Usuario).FirstOrDefaultAsync(r => r.Id == id);
            if (reserva is null) return Results.NotFound();
            if (reserva.UsuarioId != user.UsuarioActual() && !user.IsInRole("Admin"))
                return Results.NotFound();

            return Results.Ok(Mapeos.ToReservaDto(reserva));
        });

        // DELETE /api/reservas/{id} -> cancela y devuelve la ficha (mínimo 1 día antes)
        grupo.MapDelete("/{id:int}", async (int id, ClaimsPrincipal user, GastroSenaDbContext db) =>
        {
            var uid = user.UsuarioActual();
            var reserva = await db.Reservas.FirstOrDefaultAsync(r => r.Id == id && r.UsuarioId == uid);
            if (reserva is null) return Results.NotFound();

            if (reserva.Estado != EstadoReserva.Reservada)
                return Results.BadRequest(new { mensaje = "Solo se pueden cancelar reservas activas." });

            if (reserva.Fecha <= Utilidades.HoyEnColombia())
                return Results.BadRequest(new { mensaje = "Solo puedes cancelar con al menos un día de anticipación." });

            var usuario = await db.Usuarios.FindAsync(uid);
            if (usuario is null) return Results.NotFound();

            reserva.Estado = EstadoReserva.Cancelada;
            usuario.Fichas += 1;
            db.Transacciones.Add(new Transaccion
            {
                UsuarioId = uid,
                Tipo = TipoTransaccion.ReservaCancelada,
                Fichas = 1,
                Descripcion = $"Cancelación de la reserva {reserva.Id}"
            });
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }
}
