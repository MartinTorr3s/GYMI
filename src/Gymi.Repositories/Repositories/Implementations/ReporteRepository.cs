using Gymi.UseCase;
using Microsoft.EntityFrameworkCore;

namespace Gymi.Repositories;

public sealed class ReporteRepository(GymiDbContext db) : IReporteRepository
{
    public async Task<int> LeerIdMaximoAsync(TipoReporte tipo, CancellationToken ct)
    {
        var ids = tipo switch
        {
            TipoReporte.ClientesActivos => db.Clientes.Select(c => (int?)c.Id),
            TipoReporte.PagosRegistrados => db.Pagos.Select(p => (int?)p.Id),
            TipoReporte.Asistencias => db.Asistencias.Select(a => (int?)a.Id),
            _ => throw new ArgumentOutOfRangeException(nameof(tipo))
        };

        return await ids.MaxAsync(ct) ?? 0;
    }

    public async Task<IReadOnlyList<FilaClienteActivo>> ListarClientesActivosAsync(FiltroReporte filtro, CancellationToken ct)
    {
        var clientes = db.Clientes.Where(c => c.Activo && c.Id <= filtro.IdMaximo);
        if (filtro.FechaDesde is { } desde && filtro.FechaHasta is { } hasta)
        {
            clientes = clientes.Where(c => c.FechaAlta >= desde && c.FechaAlta <= hasta);
        }
        if (filtro.DespuesDe is { } ultima)
        {
            var fecha = DateOnly.FromDayNumber((int)ultima.FechaOrden);
            clientes = clientes.Where(c => c.FechaAlta < fecha || (c.FechaAlta == fecha && c.Id < ultima.Id));
        }

        return await clientes.OrderByDescending(c => c.FechaAlta).ThenByDescending(c => c.Id)
            .Take(filtro.Cantidad)
            .Select(c => new FilaClienteActivo(c.Id, c.Nombre, c.Apellido, c.FechaAlta)).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<FilaPagoRegistrado>> ListarPagosAsync(FiltroReporte filtro, CancellationToken ct)
    {
        var pagos = db.Pagos.Where(p => p.Id <= filtro.IdMaximo);
        if (filtro.FechaDesde is { } desde && filtro.FechaHasta is { } hasta)
        {
            pagos = pagos.Where(p => p.FechaPago >= desde && p.FechaPago <= hasta);
        }
        if (filtro.DespuesDe is { } ultima)
        {
            var fecha = DateOnly.FromDayNumber((int)ultima.FechaOrden);
            pagos = pagos.Where(p => p.FechaPago < fecha || (p.FechaPago == fecha && p.Id < ultima.Id));
        }

        var consulta =
            from pago in pagos
            join cliente in db.Clientes on pago.MembresiaCubierta.ClienteId equals cliente.Id
            orderby pago.FechaPago descending, pago.Id descending
            select new FilaPagoRegistrado(pago.Id, cliente.Id, pago.MembresiaOrigenId, cliente.Nombre,
                cliente.Apellido, pago.FechaPago, pago.Monto, pago.MedioPago);

        return await consulta.Take(filtro.Cantidad).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<FilaAsistencia>> ListarAsistenciasAsync(FiltroReporte filtro,
        DateTimeOffset? desdeUtc, DateTimeOffset? hastaUtcExclusiva, CancellationToken ct)
    {
        var asistencias = db.Asistencias.Where(a => a.Id <= filtro.IdMaximo);
        if (desdeUtc is { } desde)
        {
            asistencias = asistencias.Where(a => a.FechaHora >= desde);
        }
        if (hastaUtcExclusiva is { } hasta)
        {
            asistencias = asistencias.Where(a => a.FechaHora < hasta);
        }
        if (filtro.DespuesDe is { } ultima)
        {
            var fecha = new DateTimeOffset(ultima.FechaOrden, TimeSpan.Zero);
            asistencias = asistencias.Where(a => a.FechaHora < fecha || (a.FechaHora == fecha && a.Id < ultima.Id));
        }

        var consulta =
            from asistencia in asistencias
            join cliente in db.Clientes on asistencia.Membresia.ClienteId equals cliente.Id
            orderby asistencia.FechaHora descending, asistencia.Id descending
            select new FilaAsistencia(asistencia.Id, cliente.Id, cliente.Nombre, cliente.Apellido,
                asistencia.MembresiaId, asistencia.FechaHora);

        return await consulta.Take(filtro.Cantidad).ToListAsync(ct);
    }
}
