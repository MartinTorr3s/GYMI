using Gymi.UseCase;
using Gymi.Entity;
using Microsoft.EntityFrameworkCore;

namespace Gymi.Repositories;

public sealed class MembresiaRepository(GymiDbContext db) : IMembresiaRepository
{
    public Task<EstadoAsignacionMembresia> GuardarAsignacionAsync(Membresia membresia, Guid versionPlan, CancellationToken ct) =>
        db.ConEscrituraReservadaAsync(async () =>
        {
            var estadoCliente = await db.Clientes.Where(c => c.Id == membresia.ClienteId)
                .Select(c => (bool?)c.Activo).SingleOrDefaultAsync(ct);
            if (estadoCliente is null)
            {
                return EstadoAsignacionMembresia.ClienteInexistente;
            }

            if (estadoCliente == false)
            {
                return EstadoAsignacionMembresia.ClienteInactivo;
            }

            var plan = await db.Planes.Where(p => p.Id == membresia.PlanId)
                .Select(p => new { p.Activo, p.Version }).SingleOrDefaultAsync(ct);
            if (plan is null)
            {
                return EstadoAsignacionMembresia.PlanInexistente;
            }

            if (!plan.Activo)
            {
                return EstadoAsignacionMembresia.PlanInactivo;
            }

            if (plan.Version != versionPlan)
            {
                return EstadoAsignacionMembresia.PlanModificado;
            }

            if (await db.ExisteMembresiaSuperpuestaAsync(membresia.ClienteId,
                membresia.FechaInicio, membresia.FechaVencimiento, ct))
            {
                return EstadoAsignacionMembresia.PeriodoSuperpuesto;
            }

            db.Membresias.Add(membresia);
            await db.SaveChangesAsync(ct);
            return EstadoAsignacionMembresia.Creada;
        }, ct);

    public async Task<IReadOnlyList<AlertaVencimientoDto>> ListarVencidasAsync(DateOnly fecha, int offset, int cantidad,
        CancellationToken ct)
    {
        var membresias = db.Membresias.AsNoTracking();
        // Un vencimiento posterior entre los períodos iniciados desplaza la alerta del período anterior.
        var consulta =
            from vencida in membresias
            join cliente in db.Clientes on vencida.ClienteId equals cliente.Id
            join plan in db.Planes on vencida.PlanId equals plan.Id
            where cliente.Activo && vencida.FechaVencimiento <= fecha
                && !membresias.Any(otra => otra.ClienteId == vencida.ClienteId && otra.FechaInicio <= fecha
                    && (otra.FechaVencimiento > vencida.FechaVencimiento
                        || (otra.FechaVencimiento == vencida.FechaVencimiento && otra.Id > vencida.Id)))
            orderby vencida.FechaVencimiento descending, vencida.ClienteId descending
            select new AlertaVencimientoDto(cliente.Id, cliente.Nombre, cliente.Apellido, vencida.Id, plan.Id,
                plan.Nombre, vencida.FechaVencimiento);

        return await consulta.Skip(offset).Take(cantidad).ToListAsync(ct);
    }

    public Task<Membresia?> BuscarAsync(int clienteId, int id, CancellationToken ct) =>
        db.Membresias.AsNoTracking().SingleOrDefaultAsync(m => m.ClienteId == clienteId && m.Id == id, ct);

    public async Task<IReadOnlyList<Membresia>> ListarAsync(int clienteId, int offset, int cantidad, CancellationToken ct) =>
        await db.Membresias.AsNoTracking().Where(m => m.ClienteId == clienteId)
            .OrderByDescending(m => m.FechaInicio).ThenByDescending(m => m.Id)
            .Skip(offset).Take(cantidad).ToListAsync(ct);
}
