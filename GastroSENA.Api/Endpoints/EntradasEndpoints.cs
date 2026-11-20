using GastroSENA.Api.Data;
using GastroSENA.Api.Dtos;
using GastroSENA.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GastroSENA.Api.Endpoints;

public static class EntradasEndpoints
{
    public static void MapEntradasEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/entradas");

        // POST /api/entradas/reconocimiento -> lo llama el lector facial (Python), no el estudiante.
        // Requiere la cabecera X-Api-Key con la clave del dispositivo.
        grupo.MapPost("/reconocimiento", async (ReconocimientoRequest req, HttpContext http, GastroSenaDbContext db, IConfiguration config) =>
        {
            var claveEsperada = config["GastroSena:ClaveDispositivo"];
            if (string.IsNullOrEmpty(claveEsperada) ||
                !http.Request.Headers.TryGetValue("X-Api-Key", out var claveRecibida) ||
                claveRecibida.ToString() != claveEsperada)
                return Results.Unauthorized();

            var umbral = config.GetValue<double>("GastroSena:UmbralConfianza", 0.85);
            var documento = (req.Documento ?? "").Trim();
            var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Documento == documento);

            string? motivo = null;

            if (req.Confianza < umbral)
            {
                motivo = "Confianza de reconocimiento insuficiente.";
            }
            else if (usuario is null || !usuario.Activo)
            {
                motivo = "Usuario no encontrado o inactivo.";
            }
            else
            {
                var hoy = Utilidades.HoyEnColombia();
                var reserva = await db.Reservas.FirstOrDefaultAsync(r =>
                    r.UsuarioId == usuario.Id && r.Fecha == hoy && r.Estado != EstadoReserva.Cancelada);

                if (reserva is null)
                {
                    motivo = "No tienes reserva de almuerzo para hoy.";
                }
                else if (reserva.Estado == EstadoReserva.Consumida)
                {
                    motivo = "El almuerzo de hoy ya fue consumido.";
                }
                else
                {
                    reserva.Estado = EstadoReserva.Consumida;
                    reserva.FechaConsumo = DateTime.UtcNow;
                }
            }

            // Se registra el intento, sea permitido o denegado
            var entrada = new Entrada
            {
                UsuarioId = usuario?.Id,
                Documento = documento,
                FechaHora = DateTime.UtcNow,
                Confianza = req.Confianza,
                Dispositivo = req.Dispositivo ?? "",
                Resultado = motivo is null ? ResultadoEntrada.Permitida : ResultadoEntrada.Denegada,
                Motivo = motivo ?? "Acceso permitido."
            };
            db.Entradas.Add(entrada);
            await db.SaveChangesAsync();

            var dto = Mapeos.ToEntradaDto(entrada);
            return entrada.Resultado == ResultadoEntrada.Permitida
                ? Results.Ok(dto)
                : Results.Json(dto, statusCode: 403);
        });
    }
}
