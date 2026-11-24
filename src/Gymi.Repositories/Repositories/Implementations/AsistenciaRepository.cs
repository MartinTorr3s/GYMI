using Gymi.UseCase;
using Gymi.Entity;
using Microsoft.EntityFrameworkCore;

namespace Gymi.Repositories;

public sealed class AsistenciaRepository(GymiDbContext db) : IAsistenciaRepository
{
    public async Task<ConsultaAcceso?> ConsultarClienteAsync(string dni, DateOnly fecha, CancellationToken ct)
    {
        var cliente = await db.Clientes.AsNoTracking().SingleOrDefaultAsync(c => c.Dni == dni, ct);
        return cliente is null ? null
            : new ConsultaAcceso(cliente, await db.BuscarMembresiaRelevanteAsync(db.Membresias.AsNoTracking(), cliente.Id, fecha, ct));
    }

    public Task<RegistroAsistencia> RegistrarAsync(int clienteId, Guid operacionId, DateTimeOffset fechaHora,
        DateOnly fechaLocal, CancellationToken ct) =>
        db.ConEscrituraReservadaAsync<RegistroAsistencia>(async () =>
        {
            var existente = await db.Asistencias.AsNoTracking().Include(a => a.Membresia)
                .SingleOrDefaultAsync(a => a.OperacionId == operacionId, ct);
            if (existente is not null)
            {
                return existente.Membresia.ClienteId == clienteId
                    ? new(EstadoAcceso.Habilitado, existente, EsReintento: true)
                    : new(EstadoAcceso.OperacionAsignada);
            }

            var cliente = await db.Clientes.AsNoTracking().SingleOrDefaultAsync(c => c.Id == clienteId, ct);
            if (cliente is null)
            {
                return new(EstadoAcceso.ClienteInexistente);
            }

            var membresia = await db.BuscarMembresiaRelevanteAsync(db.Membresias, clienteId, fechaLocal, ct);
            var estado = new ConsultaAcceso(cliente, membresia).Evaluar(fechaLocal);
            if (estado != EstadoAcceso.Habilitado)
            {
                return new(estado);
            }

            var asistencia = membresia!.RegistrarAsistencia(operacionId, fechaHora, fechaLocal);
            db.Asistencias.Add(asistencia);
            await db.SaveChangesAsync(ct);
            return new(EstadoAcceso.Habilitado, asistencia);
        }, ct);

    public Task<Asistencia?> BuscarAsync(int clienteId, int asistenciaId, CancellationToken ct) =>
        db.Asistencias.AsNoTracking().Include(a => a.Membresia)
            .SingleOrDefaultAsync(a => a.Id == asistenciaId && a.Membresia.ClienteId == clienteId, ct);
}
