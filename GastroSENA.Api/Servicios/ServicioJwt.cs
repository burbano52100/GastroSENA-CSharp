using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GastroSENA.Api.Models;
using Microsoft.IdentityModel.Tokens;

namespace GastroSENA.Api.Servicios;

/// <summary>Genera tokens JWT firmados con HMAC-SHA256.</summary>
public class ServicioJwt(IConfiguration config)
{
    public (string Token, DateTime Expira) CrearToken(Usuario usuario)
    {
        var seccion = config.GetSection("Jwt");
        var expira = DateTime.UtcNow.AddMinutes(seccion.GetValue<int>("ExpiracionMinutos", 120));

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Name, usuario.Nombre),
            new Claim(ClaimTypes.Role, usuario.Rol.ToString())
        };

        var llave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(seccion["Clave"]!));
        var credenciales = new SigningCredentials(llave, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: seccion["Emisor"],
            audience: seccion["Audiencia"],
            claims: claims,
            expires: expira,
            signingCredentials: credenciales);

        return (new JwtSecurityTokenHandler().WriteToken(token), expira);
    }
}
