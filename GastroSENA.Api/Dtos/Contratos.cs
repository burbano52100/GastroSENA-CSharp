namespace GastroSENA.Api.Dtos;

// ----- Entradas (lo que recibe la API) -----
public record RegistroRequest(string Nombre, string Documento, string Email, string Password);
public record LoginRequest(string Email, string Password);
public record RecargaRequest(decimal Monto);
public record CompraFichasRequest(int Cantidad);
public record ReservaRequest(DateOnly Fecha);
public record ActivoRequest(bool Activo);
public record ReconocimientoRequest(string Documento, double Confianza, string Dispositivo);

// ----- Salidas (lo que devuelve la API) -----
public record TokenResponse(string Token, DateTime Expira);
public record UsuarioResponse(int Id, string Nombre, string Documento, string Email, string Rol, decimal Saldo, int Fichas);
public record ReservaResponse(int Id, string Usuario, string Documento, DateOnly Fecha, string Estado, DateTime FechaCreacion, DateTime? FechaConsumo);
public record TransaccionResponse(int Id, string Tipo, decimal Monto, int Fichas, DateTime FechaHora, string Descripcion);
public record EntradaResponse(int Id, string Documento, DateTime FechaHora, double Confianza, string Dispositivo, string Resultado, string Motivo);
public record ReservasDiaResponse(DateOnly Fecha, int Total, List<ReservaResponse> Reservas);
