using GastroSENA.Api.Data;
using GastroSENA.Api.Dtos;
using GastroSENA.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace GastroSENA.Api.Endpoints;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        // Todas las rutas requieren la política "Admin" (rol Admin en el token)
        var grupo = app.MapGroup("/api/admin").RequireAuthorization("Admin");

        // GET /api/admin/reservas?fecha=2026-11-20 -> almuerzos del día, para la cocina
        grupo.MapGet("/reservas", async (DateOnly? fecha, GastroSenaDbContext db) =>
        {
            var dia = fecha ?? Utilidades.HoyEnColombia();
            var reservas = await db.Reservas
                .Include(r => r.Usuario)
                .Where(r => r.Fecha == dia && r.Estado != EstadoReserva.Cancelada)
                .OrderBy(r => r.Id)
                .ToListAsync();
            var dtos = reservas.Select(r => Mapeos.ToReservaDto(r)).ToList();
            return Results.Ok(new ReservasDiaResponse(dia, dtos.Count, dtos));
        });

        // GET /api/admin/entradas?fecha=2026-11-20 -> registro de accesos del día
        grupo.MapGet("/entradas", async (DateOnly? fecha, GastroSenaDbContext db) =>
        {
            var dia = fecha ?? Utilidades.HoyEnColombia();
            var inicio = Utilidades.InicioDiaUtc(dia);
            var fin = inicio.AddDays(1);

            var entradas = await db.Entradas
                .Where(e => e.FechaHora >= inicio && e.FechaHora < fin)
                .OrderByDescending(e => e.FechaHora)
                .ToListAsync();
            return Results.Ok(entradas.Select(e => Mapeos.ToEntradaDto(e)).ToList());
        });

        // GET /api/admin/usuarios -> lista de usuarios
        grupo.MapGet("/usuarios", async (GastroSenaDbContext db) =>
        {
            var usuarios = await db.Usuarios.OrderBy(u => u.Nombre).ToListAsync();
            return Results.Ok(usuarios.Select(u => Mapeos.ToUsuarioDto(u)).ToList());
        });

        // PATCH /api/admin/usuarios/{id}/activo -> activa o desactiva una cuenta
        grupo.MapPatch("/usuarios/{id:int}/activo", async (int id, ActivoRequest req, GastroSenaDbContext db) =>
        {
            var usuario = await db.Usuarios.FindAsync(id);
            if (usuario is null) return Results.NotFound();

            usuario.Activo = req.Activo;
            await db.SaveChangesAsync();
            return Results.Ok(Mapeos.ToUsuarioDto(usuario));
        });
    }
}
