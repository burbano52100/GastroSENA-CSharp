using System.Security.Cryptography;

namespace GastroSENA.Api.Servicios;

/// <summary>Hash de contraseñas con PBKDF2 (incluido en .NET, sin paquetes extra).
/// Formato guardado: "iteraciones.salt.hash" (base64).</summary>
public static class ServicioPassword
{
    private const int Iteraciones = 100_000;
    private const int TamanoSalt = 16;
    private const int TamanoHash = 32;

    public static string Hashear(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(TamanoSalt);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iteraciones, HashAlgorithmName.SHA256, TamanoHash);
        return $"{Iteraciones}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public static bool Verificar(string password, string almacenado)
    {
        var partes = almacenado.Split('.');
        if (partes.Length != 3) return false;

        var iteraciones = int.Parse(partes[0]);
        var salt = Convert.FromBase64String(partes[1]);
        var esperado = Convert.FromBase64String(partes[2]);

        var calculado = Rfc2898DeriveBytes.Pbkdf2(password, salt, iteraciones, HashAlgorithmName.SHA256, esperado.Length);
        return CryptographicOperations.FixedTimeEquals(calculado, esperado);
    }
}
