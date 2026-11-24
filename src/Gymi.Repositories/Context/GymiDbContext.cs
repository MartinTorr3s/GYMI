using Gymi.UseCase;
using Gymi.Entity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Gymi.Repositories;

public sealed class GymiDbContext(DbContextOptions<GymiDbContext> options) : DbContext(options)
{
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<PlanMembresia> Planes => Set<PlanMembresia>();
    public DbSet<Membresia> Membresias => Set<Membresia>();
    public DbSet<Pago> Pagos => Set<Pago>();
    public DbSet<Asistencia> Asistencias => Set<Asistencia>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
        optionsBuilder.AddInterceptors(FuncionesSqlite.Instancia);

    public async Task<bool> MigrarBaseLocalAsync()
    {
        try
        {
            await Database.MigrateAsync();
            return true;
        }
        catch (SqliteException ex) when (ex.SqliteExtendedErrorCode == 2067 &&
            ex.Message.Contains("UNIQUE constraint failed: CorreosAccesoMigracion.Clave", StringComparison.Ordinal))
        {
            return false;
        }
    }

    internal Task<bool> ExisteMembresiaSuperpuestaAsync(int clienteId, DateOnly inicio, DateOnly vencimiento,
        CancellationToken ct) =>
        Membresias.AnyAsync(m => m.ClienteId == clienteId && m.FechaInicio < vencimiento && inicio < m.FechaVencimiento, ct);

    internal async Task<Membresia?> BuscarMembresiaRelevanteAsync(IQueryable<Membresia> membresias, int clienteId,
        DateOnly fecha, CancellationToken ct)
    {
        var periodos = membresias.Where(m => m.ClienteId == clienteId);
        // Como los períodos son excluyentes, el último que empezó es el vigente o el último vencido.
        var ultimoIniciado = await periodos.Where(m => m.FechaInicio <= fecha).OrderByDescending(m => m.FechaInicio)
            .FirstOrDefaultAsync(ct);
        return ultimoIniciado ?? await periodos.OrderBy(m => m.FechaInicio).FirstOrDefaultAsync(ct);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDbFunction(() => FuncionesSqlite.ContieneSinTildes(null!, null!))
            .HasName(FuncionesSqlite.NombreContieneSinTildes);

        var usuario = modelBuilder.Entity<Usuario>();
        usuario.UseTptMappingStrategy();
        usuario.ToTable("Usuarios", tabla =>
        {
            tabla.HasCheckConstraint("CK_Usuarios_Nombre", $"length(Nombre) BETWEEN 1 AND {LimitesCliente.Nombre}");
            tabla.HasCheckConstraint("CK_Usuarios_Apellido", $"length(Apellido) BETWEEN 1 AND {LimitesCliente.Apellido}");
            tabla.HasCheckConstraint("CK_Usuarios_Email", $"length(Email) BETWEEN 1 AND {LimitesCliente.Email}");
            tabla.HasCheckConstraint("CK_Usuarios_PasswordHash", "PasswordHash IS NULL OR (length(PasswordHash) <= 255 AND length(trim(PasswordHash)) > 0)");
        });
        usuario.HasKey(u => u.Id);
        usuario.Property(u => u.Id).UseAutoincrement();
        usuario.Property(u => u.Nombre).IsRequired().HasMaxLength(LimitesCliente.Nombre);
        usuario.Property(u => u.Apellido).IsRequired().HasMaxLength(LimitesCliente.Apellido);
        usuario.Property(u => u.Email).IsRequired().HasMaxLength(LimitesCliente.Email);
        usuario.Property(u => u.PasswordHash).HasMaxLength(255);
        usuario.Property(u => u.EmailAccesoNormalizado).HasMaxLength(LimitesCliente.Email);
        usuario.HasIndex(u => u.EmailAccesoNormalizado).IsUnique();
        usuario.Ignore(u => u.Rol);
        usuario.Property(u => u.Version).IsConcurrencyToken();
        usuario.Ignore(u => u.AccesoWebHabilitado);

        modelBuilder.Entity<Administrador>().ToTable("Administradores");
        modelBuilder.Entity<Recepcionista>().ToTable("Recepcionistas");

        var cliente = modelBuilder.Entity<Cliente>();
        cliente.ToTable("Clientes", tabla =>
        {
            tabla.HasCheckConstraint("CK_Clientes_Dni",
                $"length(Dni) BETWEEN 1 AND {LimitesCliente.Dni} AND Dni NOT GLOB '*[^0-9]*'");
            tabla.HasCheckConstraint("CK_Clientes_Telefono",
                $"(length(Telefono) BETWEEN 1 AND {LimitesCliente.TelefonoDigitos} AND Telefono NOT GLOB '*[^0-9]*') " +
                $"OR (substr(Telefono, 1, 1) = '+' AND length(Telefono) BETWEEN 2 AND {LimitesCliente.TelefonoDigitos + 1} " +
                "AND substr(Telefono, 2) NOT GLOB '*[^0-9]*')");
        });
        cliente.HasIndex(c => c.Dni).IsUnique();
        cliente.HasIndex(c => c.FechaAlta);
        cliente.Property(c => c.Dni).IsRequired().HasMaxLength(LimitesCliente.Dni);
        cliente.Property(c => c.Telefono).IsRequired().HasMaxLength(LimitesCliente.TelefonoDigitos + 1);

        var plan = modelBuilder.Entity<PlanMembresia>();
        plan.ToTable("Planes", tabla =>
        {
            tabla.HasCheckConstraint("CK_Planes_Nombre",
                $"length(Nombre) BETWEEN 1 AND {PlanMembresia.NombreMaximo} AND length(trim(Nombre)) > 0");
            tabla.HasCheckConstraint("CK_Planes_Descripcion",
                $"length(Descripcion) BETWEEN 1 AND {PlanMembresia.DescripcionMaxima} AND length(trim(Descripcion)) > 0");
            tabla.HasCheckConstraint("CK_Planes_Precio",
                $"typeof(PrecioCentavos) = 'integer' AND PrecioCentavos BETWEEN 0 AND {(long)(PlanMembresia.PrecioMaximo * 100m)}");
            tabla.HasCheckConstraint("CK_Planes_Vigencia",
                $"DuracionDias IS NULL OR (typeof(DuracionDias) = 'integer' AND DuracionDias BETWEEN 1 AND {int.MaxValue})");
            tabla.HasCheckConstraint("CK_Planes_Sesiones",
                $"typeof(CantidadSesiones) = 'integer' AND CantidadSesiones BETWEEN 1 AND {int.MaxValue}");
            tabla.HasCheckConstraint("CK_Planes_Activo", "Activo IN (0, 1)");
        });
        plan.HasKey(p => p.Id);
        plan.Property(p => p.Nombre).IsRequired().HasMaxLength(PlanMembresia.NombreMaximo);
        plan.Property(p => p.Descripcion).IsRequired().HasMaxLength(PlanMembresia.DescripcionMaxima);
        plan.Property(p => p.Precio).HasColumnName("PrecioCentavos")
            .HasConversion(precio => (long)(precio * 100m), centavos => centavos / 100m);
        plan.Property(p => p.Version).IsConcurrencyToken();

        var membresia = modelBuilder.Entity<Membresia>();
        membresia.ToTable("Membresias", tabla =>
        {
            tabla.HasCheckConstraint("CK_Membresias_Vigencia", "FechaInicio < FechaVencimiento");
            tabla.HasCheckConstraint("CK_Membresias_DiaAnclaMensual",
                "DiaAnclaMensual IS NULL OR (typeof(DiaAnclaMensual) = 'integer' AND DiaAnclaMensual BETWEEN 1 AND 31)");
            tabla.HasCheckConstraint("CK_Membresias_Cupo",
                $"typeof(SesionesIncluidas) = 'integer' AND SesionesIncluidas BETWEEN 1 AND {int.MaxValue}");
            tabla.HasCheckConstraint("CK_Membresias_Saldo",
                "typeof(SesionesDisponibles) = 'integer' AND SesionesDisponibles BETWEEN 0 AND SesionesIncluidas");
        });
        membresia.HasKey(m => m.Id);
        membresia.HasIndex(m => new { m.ClienteId, m.FechaInicio, m.Id });
        membresia.HasIndex(m => new { m.FechaVencimiento, m.ClienteId });
        membresia.HasOne<Cliente>().WithMany().HasForeignKey(m => m.ClienteId).OnDelete(DeleteBehavior.Restrict);
        membresia.HasOne<PlanMembresia>().WithMany().HasForeignKey(m => m.PlanId).OnDelete(DeleteBehavior.Restrict);

        var pago = modelBuilder.Entity<Pago>();
        pago.ToTable("Pagos", tabla =>
        {
            tabla.HasCheckConstraint("CK_Pagos_Monto",
                $"typeof(MontoCentavos) = 'integer' AND MontoCentavos BETWEEN 1 AND {(long)(Pago.MontoMaximo * 100m)}");
            tabla.HasCheckConstraint("CK_Pagos_MedioPago",
                $"length(MedioPago) BETWEEN 1 AND {Pago.MedioPagoMaximo} AND length(trim(MedioPago)) > 0");
            tabla.HasCheckConstraint("CK_Pagos_Observacion",
                $"Observacion IS NULL OR (length(Observacion) <= {Pago.ObservacionMaxima} AND length(trim(Observacion)) > 0)");
            tabla.HasCheckConstraint("CK_Pagos_TipoYPeriodo",
                $"(Tipo = {(int)TipoPago.PrimerPago} AND MembresiaOrigenId = MembresiaCubiertaId) OR " +
                $"(Tipo = {(int)TipoPago.Renovacion} AND MembresiaOrigenId <> MembresiaCubiertaId)");
        });
        pago.HasKey(p => p.Id);
        pago.HasIndex(p => p.FechaPago);
        pago.HasIndex(p => p.MembresiaCubiertaId).IsUnique();
        pago.HasIndex(p => p.MembresiaOrigenId).IsUnique().HasFilter($"Tipo = {(int)TipoPago.Renovacion}");
        pago.Property(p => p.Monto).HasColumnName("MontoCentavos")
            .HasConversion(monto => (long)(monto * 100m), centavos => centavos / 100m);
        pago.Property(p => p.MedioPago).IsRequired().HasMaxLength(Pago.MedioPagoMaximo);
        pago.Property(p => p.Observacion).HasMaxLength(Pago.ObservacionMaxima);
        pago.HasOne<Membresia>().WithMany().HasForeignKey(p => p.MembresiaOrigenId).OnDelete(DeleteBehavior.Restrict);
        pago.HasOne(p => p.MembresiaCubierta).WithMany().HasForeignKey(p => p.MembresiaCubiertaId).OnDelete(DeleteBehavior.Restrict);

        var asistencia = modelBuilder.Entity<Asistencia>();
        asistencia.ToTable("Asistencias", tabla =>
        {
            tabla.HasCheckConstraint("CK_Asistencias_Operacion",
                $"length(OperacionId) = 36 AND OperacionId <> '{Guid.Empty:D}'");
            tabla.HasCheckConstraint("CK_Asistencias_FechaHora",
                $"typeof(FechaHoraUtcTicks) = 'integer' AND FechaHoraUtcTicks BETWEEN 0 AND {DateTime.MaxValue.Ticks}");
            tabla.HasCheckConstraint("CK_Asistencias_SesionesRestantes",
                $"typeof(SesionesRestantes) = 'integer' AND SesionesRestantes BETWEEN 0 AND {int.MaxValue}");
        });
        asistencia.HasKey(a => a.Id);
        asistencia.HasIndex(a => a.FechaHora);
        asistencia.HasIndex(a => a.OperacionId).IsUnique();
        asistencia.Property(a => a.OperacionId).UseCollation("NOCASE");
        asistencia.Property(a => a.FechaHora).HasColumnName("FechaHoraUtcTicks")
            .HasConversion(fecha => fecha.UtcDateTime.Ticks, ticks => new DateTimeOffset(ticks, TimeSpan.Zero));
        asistencia.HasOne(a => a.Membresia).WithMany().HasForeignKey(a => a.MembresiaId).OnDelete(DeleteBehavior.Restrict);

        var activacion = modelBuilder.Entity<ActivacionUsuario>();
        activacion.ToTable("ActivacionesUsuario", tabla =>
            tabla.HasCheckConstraint("CK_ActivacionesUsuario_TokenHash", "length(TokenHash) = 64"));
        activacion.HasKey(a => a.UsuarioId);
        activacion.HasIndex(a => a.TokenHash).IsUnique();
        activacion.Property(a => a.TokenHash).HasMaxLength(64);
        activacion.Property(a => a.VenceEn).HasColumnName("VenceEnUtcTicks")
            .HasConversion(fecha => fecha.UtcDateTime.Ticks, ticks => new DateTimeOffset(ticks, TimeSpan.Zero));
        activacion.HasOne(a => a.Usuario).WithOne().HasForeignKey<ActivacionUsuario>(a => a.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        var sesion = modelBuilder.Entity<SesionUsuario>();
        sesion.ToTable("SesionesUsuario", tabla =>
            tabla.HasCheckConstraint("CK_SesionesUsuario_TokenHash", "length(TokenHash) = 64"));
        sesion.HasKey(s => s.TokenHash);
        sesion.Property(s => s.TokenHash).HasMaxLength(64);
        sesion.HasIndex(s => s.VenceEn);
        sesion.Property(s => s.VenceEn).HasColumnName("VenceEnUtcTicks")
            .HasConversion(fecha => fecha.UtcDateTime.Ticks, ticks => new DateTimeOffset(ticks, TimeSpan.Zero));
        sesion.HasOne(s => s.Usuario).WithMany().HasForeignKey(s => s.UsuarioId).OnDelete(DeleteBehavior.Restrict);
    }
}
