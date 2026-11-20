using GastroSENA.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace GastroSENA.Api.Data;

public class GastroSenaDbContext(DbContextOptions<GastroSenaDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Reserva> Reservas => Set<Reserva>();
    public DbSet<Entrada> Entradas => Set<Entrada>();
    public DbSet<Transaccion> Transacciones => Set<Transaccion>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Usuario>(e =>
        {
            e.HasIndex(u => u.Documento).IsUnique();
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Saldo).HasPrecision(12, 2);
        });

        mb.Entity<Reserva>(e =>
        {
            e.HasOne(r => r.Usuario).WithMany(u => u.Reservas)
             .HasForeignKey(r => r.UsuarioId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(r => new { r.UsuarioId, r.Fecha });
        });

        mb.Entity<Entrada>(e =>
        {
            e.HasOne(x => x.Usuario).WithMany()
             .HasForeignKey(x => x.UsuarioId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        mb.Entity<Transaccion>(e =>
        {
            e.Property(t => t.Monto).HasPrecision(12, 2);
            e.HasOne(t => t.Usuario).WithMany()
             .HasForeignKey(t => t.UsuarioId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // Npgsql 6+ no escribe DateTime con Kind=Utc en columnas "timestamp without time zone".
        // Guardamos todas las fechas como "timestamp with time zone".
        foreach (var propiedad in mb.Model.GetEntityTypes()
                     .SelectMany(t => t.GetProperties())
                     .Where(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?)))
        {
            propiedad.SetColumnType("timestamp with time zone");
        }
    }
}
