using GastroSENA.Api.Data;
using GastroSENA.Api.Dtos;
using GastroSENA.Api.Models;
using GastroSENA.Api.Servicios;
using Microsoft.EntityFrameworkCore;

namespace GastroSENA.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/auth");

        // POST /api/auth/registro -> crea una cuenta de estudiante (público)
        grupo.MapPost("/registro", async (RegistroRequest req, GastroSenaDbContext db) =>
        {
            var email = (req.Email ?? "").Trim().ToLowerInvariant();
            var documento = (req.Documento ?? "").Trim();

            if (string.IsNullOrWhiteSpace(req.Nombre) || string.IsNullOrWhiteSpace(documento) || !email.Contains('@'))
                return Results.BadRequest(new { mensaje = "Nombre, documento y un email válido son obligatorios." });

            if (string.IsNullOrEmpty(req.Password) || req.Password.Length < 8)
                return Results.BadRequest(new { mensaje = "La contraseña debe tener al menos 8 caracteres." });

            if (await db.Usuarios.AnyAsync(u => u.Email == email || u.Documento == documento))
                return Results.Conflict(new { mensaje = "Ya existe un usuario con ese email o documento." });

            var usuario = new Usuario
            {
                Nombre = req.Nombre.Trim(),
                Documento = documento,
                Email = email,
                PasswordHash = ServicioPassword.Hashear(req.Password)
            };
            db.Usuarios.Add(usuario);
            await db.SaveChangesAsync();

            return Results.Created("/api/usuarios/me", Mapeos.ToUsuarioDto(usuario));
        });

        // POST /api/auth/login -> devuelve un token JWT (público)
        grupo.MapPost("/login", async (LoginRequest req, GastroSenaDbContext db, ServicioJwt jwt) =>
        {
            var email = (req.Email ?? "").Trim().ToLowerInvariant();
            var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Email == email);

            if (usuario is null || !ServicioPassword.Verificar(req.Password ?? "", usuario.PasswordHash))
                return Results.Unauthorized();

            if (!usuario.Activo)
                return Results.Json(new { mensaje = "Usuario inactivo." }, statusCode: 403);

            var (token, expira) = jwt.CrearToken(usuario);
            return Results.Ok(new TokenResponse(token, expira));
        });
    }
}
