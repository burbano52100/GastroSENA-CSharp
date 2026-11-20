namespace GastroSENA.Api.Models;

public enum RolUsuario { Estudiante = 0, Admin = 1 }

public enum EstadoReserva { Reservada = 0, Consumida = 1, Cancelada = 2 }

public enum TipoTransaccion { Recarga = 0, CompraFichas = 1, ReservaCreada = 2, ReservaCancelada = 3 }

public enum ResultadoEntrada { Permitida = 0, Denegada = 1 }

public class Usuario
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Documento { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public RolUsuario Rol { get; set; } = RolUsuario.Estudiante;
    public decimal Saldo { get; set; }
    public int Fichas { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
    public List<Reserva> Reservas { get; set; } = new();
}

public class Reserva
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
    public DateOnly Fecha { get; set; }
    public EstadoReserva Estado { get; set; } = EstadoReserva.Reservada;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaConsumo { get; set; }
}

public class Entrada
{
    public int Id { get; set; }
    public int? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
    public string Documento { get; set; } = "";
    public DateTime FechaHora { get; set; } = DateTime.UtcNow;
    public double Confianza { get; set; }
    public string Dispositivo { get; set; } = "";
    public ResultadoEntrada Resultado { get; set; }
    public string Motivo { get; set; } = "";
}

public class Transaccion
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
    public TipoTransaccion Tipo { get; set; }
    public decimal Monto { get; set; }
    public int Fichas { get; set; }
    public DateTime FechaHora { get; set; } = DateTime.UtcNow;
    public string Descripcion { get; set; } = "";
}
