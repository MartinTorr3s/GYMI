using Gymi.Entity;

namespace Gymi.UseCase;

public sealed record DatosRegistroPago(string? Tipo, DateOnly? FechaPago, decimal? Monto, string? MedioPago,
    string? Observacion);

public sealed record PagoValidado(TipoPago Tipo, DateOnly FechaPago, decimal Monto, string MedioPago, string? Observacion);

public sealed record PagoDto(int Id, int ClienteId, int MembresiaOrigenId, string Tipo, DateOnly FechaPago,
    decimal Monto, string MedioPago, string? Observacion, MembresiaDto MembresiaCubierta)
{
    public static PagoDto Desde(Pago pago, DateOnly hoy) =>
        new(pago.Id, pago.MembresiaCubierta.ClienteId, pago.MembresiaOrigenId, pago.Tipo switch
        {
            TipoPago.PrimerPago => "primerPago",
            TipoPago.Renovacion => "renovacion",
            _ => throw new InvalidOperationException("Tipo de pago desconocido.")
        }, pago.FechaPago, pago.Monto, pago.MedioPago, pago.Observacion, MembresiaDto.Desde(pago.MembresiaCubierta, hoy));
}

public enum EstadoRegistroPago
{
    Registrado,
    ClienteInexistente,
    ClienteInactivo,
    MembresiaInexistente,
    MembresiaPagada,
    MembresiaRenovada,
    PlanInactivo,
    PeriodoSuperpuesto,
    VencimientoFueraDeRango
}

public sealed record ReferenciaPago(int PagoId, int MembresiaOrigenId);

public sealed record RegistroPago(EstadoRegistroPago Estado, Pago? Pago = null, ReferenciaPago? Existente = null);
