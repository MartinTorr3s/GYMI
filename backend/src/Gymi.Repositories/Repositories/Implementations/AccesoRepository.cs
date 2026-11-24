using Gymi.UseCase;
using Gymi.Entity;
using Microsoft.EntityFrameworkCore;

namespace Gymi.Repositories;

public sealed class AccesoRepository(GymiDbContext db) : IAccesoRepository
{
    public Task<Usuario?> BuscarUsuarioAsync(string emailNormalizado, CancellationToken ct) =>
        db.Set<Usuario>().AsNoTracking().SingleOrDefaultAsync(u => u.EmailAccesoNormalizado == emailNormalizado, ct);

    public Task<bool> CrearSesionAsync(SesionUsuario sesion, DateTimeOffset ahora, CancellationToken ct) =>
        db.ConEscrituraReservadaAsync(async () =>
        {
            var vigente = await db.Set<Usuario>().AnyAsync(u => u.Id == sesion.UsuarioId && u.Activo
                && u.Version == sesion.VersionUsuario && u.EmailAccesoNormalizado != null && u.PasswordHash != null, ct);
            if (!vigente)
            {
                return false;
            }

            await db.Set<SesionUsuario>().Where(s => s.VenceEn <= ahora).ExecuteDeleteAsync(ct);
            db.Set<SesionUsuario>().Add(sesion);
            await db.SaveChangesAsync(ct);
            return true;
        }, ct);

    public Task<Usuario?> BuscarSesionAsync(string tokenHash, DateTimeOffset ahora, CancellationToken ct) =>
        db.Set<SesionUsuario>().AsNoTracking()
            .Where(s => s.TokenHash == tokenHash && s.VenceEn > ahora && s.Usuario.Activo
                && s.Usuario.Version == s.VersionUsuario && s.Usuario.PasswordHash != null && s.Usuario.EmailAccesoNormalizado != null)
            .Select(s => s.Usuario).SingleOrDefaultAsync(ct);

    public Task CerrarSesionAsync(string tokenHash, CancellationToken ct) =>
        db.Set<SesionUsuario>().Where(s => s.TokenHash == tokenHash).ExecuteDeleteAsync(ct);

    public Task<EstadoCuenta> GuardarInvitacionAsync(int id, TipoInvitacion tipo, string tokenHash, DateTimeOffset venceEn,
        CancellationToken ct) =>
        db.ConEscrituraReservadaAsync(async () =>
        {
            IQueryable<Usuario> destinatarios = tipo switch
            {
                TipoInvitacion.Cliente => db.Clientes,
                TipoInvitacion.Recepcionista => db.Set<Recepcionista>(),
                _ => throw new ArgumentOutOfRangeException(nameof(tipo))
            };
            var usuario = await destinatarios.AsNoTracking().SingleOrDefaultAsync(u => u.Id == id, ct);
            if (usuario is null)
            {
                return tipo == TipoInvitacion.Cliente ? EstadoCuenta.ClienteInexistente : EstadoCuenta.RecepcionistaInexistente;
            }

            if (!usuario.Activo)
            {
                return tipo == TipoInvitacion.Cliente ? EstadoCuenta.ClienteInactivo : EstadoCuenta.RecepcionistaInactivo;
            }

            if (usuario.AccesoWebHabilitado)
            {
                return EstadoCuenta.AccesoYaHabilitado;
            }

            if (await EmailOcupadoAsync(usuario, ct))
            {
                return EstadoCuenta.EmailOcupado;
            }

            var activaciones = db.Set<ActivacionUsuario>();
            await activaciones.Where(a => a.UsuarioId == id).ExecuteDeleteAsync(ct);
            activaciones.Add(new(usuario, tokenHash, venceEn));
            await db.SaveChangesAsync(ct);
            return EstadoCuenta.Guardado;
        }, ct);

    public Task<EstadoCuenta> ActivarAsync(string tokenHash, string passwordHash, DateTimeOffset ahora, CancellationToken ct) =>
        db.ConEscrituraReservadaAsync(async () =>
        {
            var activaciones = db.Set<ActivacionUsuario>();
            var activacion = await activaciones.Include(a => a.Usuario)
                .SingleOrDefaultAsync(a => a.TokenHash == tokenHash && a.VenceEn > ahora, ct);
            if (activacion?.Usuario is not (Cliente or Recepcionista) || !activacion.Usuario.Activo
                || activacion.Usuario.AccesoWebHabilitado || activacion.Usuario.Version != activacion.VersionUsuario)
            {
                return EstadoCuenta.ActivacionInvalida;
            }

            var usuario = activacion.Usuario;
            if (await EmailOcupadoAsync(usuario, ct))
            {
                return EstadoCuenta.EmailOcupado;
            }

            usuario.HabilitarAcceso(passwordHash);
            activaciones.Remove(activacion);
            await db.SaveChangesAsync(ct);
            return EstadoCuenta.Guardado;
        }, ct);

    public Task<EstadoCuenta> CrearPrimerAdministradorAsync(Administrador administrador, CancellationToken ct) =>
        db.ConEscrituraReservadaAsync(async () =>
        {
            var administradores = db.Set<Administrador>();
            if (await administradores.AnyAsync(u => u.EmailAccesoNormalizado != null, ct))
            {
                return EstadoCuenta.AdministradorExistente;
            }

            if (await EmailOcupadoAsync(administrador, ct))
            {
                return EstadoCuenta.EmailOcupado;
            }

            administradores.Add(administrador);
            await db.SaveChangesAsync(ct);
            return EstadoCuenta.Guardado;
        }, ct);

    public async Task<ConsultaMembresiaPropia?> BuscarMembresiaPropiaAsync(int clienteId, DateOnly fecha, CancellationToken ct)
    {
        var cliente = await db.Clientes.AsNoTracking().SingleOrDefaultAsync(c => c.Id == clienteId && c.Activo, ct);
        if (cliente is null)
        {
            return null;
        }

        var membresia = await db.BuscarMembresiaRelevanteAsync(db.Membresias.AsNoTracking(), clienteId, fecha, ct);
        var nombrePlan = membresia is null ? null : await db.Planes.Where(p => p.Id == membresia.PlanId)
            .Select(p => p.Nombre).SingleAsync(ct);
        return new(cliente, membresia, nombrePlan);
    }

    public async Task<IReadOnlyList<PagoCuentaDto>> BuscarPagosPropiosAsync(int clienteId, int offset, int cantidad,
        CancellationToken ct) =>
        await db.Pagos.Where(p => p.MembresiaCubierta.ClienteId == clienteId)
            .OrderByDescending(p => p.FechaPago).ThenByDescending(p => p.Id)
            .Skip(offset).Take(cantidad).Select(p => new PagoCuentaDto(p.FechaPago, p.Monto, p.MedioPago)).ToListAsync(ct);

    private Task<bool> EmailOcupadoAsync(Usuario usuario, CancellationToken ct)
    {
        var emailNormalizado = Usuario.NormalizarEmail(usuario.Email);
        return db.Set<Usuario>().AnyAsync(u => u.EmailAccesoNormalizado == emailNormalizado && u.Id != usuario.Id, ct);
    }
}
