using GastroSENA.Api.Models;
using GastroSENA.Api.Servicios;
using Microsoft.EntityFrameworkCore;

namespace GastroSENA.Api.Data;

public static class DatosIniciales
{
    /// <summary>Crea el administrador inicial si Admin:Email y Admin:Password están configurados.</summary>
    public static async Task SembrarAdminAsync(GastroSenaDbContext db, IConfiguration config)
    {
        var email = config["Admin:Email"]?.Trim().ToLowerInvariant();
        var password = config["Admin:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)) return;
        if (await db.Usuarios.AnyAsync(u => u.Email == email)) return;

        db.Usuarios.Add(new Usuario
        {
            Nombre = "Administrador",
            Documento = "ADMIN",
            Email = email,
            PasswordHash = ServicioPassword.Hashear(password),
            Rol = RolUsuario.Admin
        });
        await db.SaveChangesAsync();
    }
}
