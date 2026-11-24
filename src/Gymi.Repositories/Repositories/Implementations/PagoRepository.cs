using Gymi.UseCase;
using Gymi.Entity;
using Microsoft.EntityFrameworkCore;

namespace Gymi.Repositories;

public sealed class PagoRepository(GymiDbContext db) : IPagoRepository
{
    public Task<RegistroPago> RegistrarAsync(int clienteId, int membresiaId, PagoValidado datos, CancellationToken ct) =>
        db.ConEscrituraReservadaAsync(async () =>
        {
            var clienteActivo = await db.Clientes.Where(c => c.Id == clienteId)
                .Select(c => (bool?)c.Activo).SingleOrDefaultAsync(ct);
            if (clienteActivo is null)
            {
                return new(EstadoRegistroPago.ClienteInexistente);
            }

            var origen = await db.Membresias.SingleOrDefaultAsync(m => m.ClienteId == clienteId && m.Id == membresiaId, ct);
            if (origen is null)
            {
                return new(clienteActivo.Value ? EstadoRegistroPago.MembresiaInexistente : EstadoRegistroPago.ClienteInactivo);
            }

            var esPrimerPago = datos.Tipo == TipoPago.PrimerPago;
            var pagosExistentes = esPrimerPago
                ? db.Pagos.Where(p => p.MembresiaCubiertaId == origen.Id)
                : db.Pagos.Where(p => p.MembresiaOrigenId == origen.Id && p.Tipo == TipoPago.Renovacion);
            var existente = await pagosExistentes
                .Select(p => new ReferenciaPago(p.Id, p.MembresiaOrigenId)).SingleOrDefaultAsync(ct);
            if (existente is not null)
            {
                var estado = esPrimerPago ? EstadoRegistroPago.MembresiaPagada : EstadoRegistroPago.MembresiaRenovada;
                return new(estado, Existente: existente);
            }

            if (!clienteActivo.Value)
            {
                return new(EstadoRegistroPago.ClienteInactivo);
            }

            var registro = esPrimerPago
                ? new RegistroPago(EstadoRegistroPago.Registrado,
                    new Pago(origen, origen, datos.Tipo, datos.FechaPago, datos.Monto, datos.MedioPago, datos.Observacion))
                : await RegistrarRenovacionAsync(origen, datos, ct);
            if (registro.Pago is not { } pago)
            {
                return registro;
            }

            db.Pagos.Add(pago);
            await db.SaveChangesAsync(ct);
            return registro;
        }, ct);

    public Task<Pago?> BuscarAsync(int clienteId, int membresiaId, int pagoId, CancellationToken ct) =>
        db.Pagos.AsNoTracking().Include(p => p.MembresiaCubierta)
            .SingleOrDefaultAsync(p => p.Id == pagoId && p.MembresiaOrigenId == membresiaId
                && p.MembresiaCubierta.ClienteId == clienteId, ct);

    private async Task<RegistroPago> RegistrarRenovacionAsync(Membresia origen, PagoValidado datos,
        CancellationToken ct)
    {
        var plan = await db.Planes.AsNoTracking().SingleAsync(p => p.Id == origen.PlanId, ct);
        if (!plan.Activo)
        {
            return new(EstadoRegistroPago.PlanInactivo);
        }

        var inicio = origen.InicioDeRenovacion(datos.FechaPago);
        var diaAncla = origen.DiaAnclaDeRenovacion(plan, inicio);
        if (Membresia.CalcularVencimiento(inicio, plan, diaAncla) is not { } vencimiento)
        {
            return new(EstadoRegistroPago.VencimientoFueraDeRango);
        }

        if (await db.ExisteMembresiaSuperpuestaAsync(origen.ClienteId, inicio, vencimiento, ct))
        {
            return new(EstadoRegistroPago.PeriodoSuperpuesto);
        }

        var cubierta = new Membresia(origen.ClienteId, plan, inicio, vencimiento, diaAncla);
        return new(EstadoRegistroPago.Registrado,
            new Pago(origen, cubierta, datos.Tipo, datos.FechaPago, datos.Monto, datos.MedioPago, datos.Observacion));
    }
}
