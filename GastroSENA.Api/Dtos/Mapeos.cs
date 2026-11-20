using GastroSENA.Api.Models;

namespace GastroSENA.Api.Dtos;

/// <summary>Convierte entidades en respuestas, para no exponer el hash de contraseña.</summary>
public static class Mapeos
{
    public static UsuarioResponse ToUsuarioDto(Usuario u) =>
        new(u.Id, u.Nombre, u.Documento, u.Email, u.Rol.ToString(), u.Saldo, u.Fichas);

    public static ReservaResponse ToReservaDto(Reserva r) =>
        new(r.Id, r.Usuario?.Nombre ?? "", r.Usuario?.Documento ?? "", r.Fecha,
            r.Estado.ToString(), r.FechaCreacion, r.FechaConsumo);

    public static TransaccionResponse ToTransaccionDto(Transaccion t) =>
        new(t.Id, t.Tipo.ToString(), t.Monto, t.Fichas, t.FechaHora, t.Descripcion);

    public static EntradaResponse ToEntradaDto(Entrada e) =>
        new(e.Id, e.Documento, e.FechaHora, e.Confianza, e.Dispositivo, e.Resultado.ToString(), e.Motivo);
}
