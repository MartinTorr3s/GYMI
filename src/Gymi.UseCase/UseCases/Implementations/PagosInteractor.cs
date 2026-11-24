using Gymi.UseCase;
using Gymi.Entity;

namespace Gymi.UseCase.UseCases.Implementations;

public sealed class PagosInteractor(IPagoRepository repository, IPresentador<PagoDto> salida,
    TimeProvider reloj, TimeZoneInfo zonaHoraria) : IPagosEntrada
{
    public async Task RegistrarAsync(int clienteId, int membresiaId, DatosRegistroPago datos, CancellationToken ct)
    {
        var hoy = Hoy;
        var errores = ValidarRuta(clienteId, membresiaId);
        var tipo = datos.Tipo switch
        {
            "primerPago" => TipoPago.PrimerPago,
            "renovacion" => TipoPago.Renovacion,
            _ => (TipoPago?)null
        };

        if (tipo is null)
        {
            errores["tipo"] = ["Elegí primerPago o renovacion."];
        }

        if (datos.FechaPago is null || datos.FechaPago > hoy)
        {
            errores["fechaPago"] = ["Ingresá la fecha del pago. Debe ser hoy o una fecha anterior."];
        }

        if (datos.Monto is not { } monto || !Pago.EsMontoValido(monto))
        {
            errores["monto"] = ["El monto debe estar entre 0,01 y 99.999.999,99, con hasta dos decimales."];
        }

        var medioPago = ValidacionEntrada.Texto(datos.MedioPago, "medioPago", "El medio de pago", Pago.MedioPagoMaximo, errores);
        var observacion = string.IsNullOrWhiteSpace(datos.Observacion) ? null
            : ValidacionEntrada.Texto(datos.Observacion, "observacion", "La observación", Pago.ObservacionMaxima, errores);

        if (errores.Count > 0)
        {
            salida.Presentar(Resultado<PagoDto>.Fallo(ValidacionEntrada.Error(errores)));
            return;
        }

        var registro = await repository.RegistrarAsync(clienteId, membresiaId,
            new PagoValidado(tipo!.Value, datos.FechaPago!.Value, datos.Monto!.Value, medioPago, observacion), ct);
        if (registro.Estado == EstadoRegistroPago.Registrado)
        {
            salida.Presentar(Resultado<PagoDto>.Exito(PagoDto.Desde(registro.Pago!, hoy)));
            return;
        }

        var error = ErrorDeRegistro(registro.Estado);
        if (registro.Existente is { } pago)
        {
            error = error with
            {
                Detalles = new Dictionary<string, object?>
                {
                    ["pagoId"] = pago.PagoId,
                    ["membresiaOrigenId"] = pago.MembresiaOrigenId
                }
            };
        }

        salida.Presentar(Resultado<PagoDto>.Fallo(error));
    }

    public async Task ObtenerAsync(int clienteId, int membresiaId, int pagoId, CancellationToken ct)
    {
        var errores = ValidarRuta(clienteId, membresiaId);
        if (pagoId <= 0)
        {
            errores["pagoId"] = ["El ID del pago debe ser un entero positivo."];
        }

        if (errores.Count > 0)
        {
            salida.Presentar(Resultado<PagoDto>.Fallo(ValidacionEntrada.Error(errores)));
            return;
        }

        var pago = await repository.BuscarAsync(clienteId, membresiaId, pagoId, ct);
        salida.Presentar(pago is null
            ? Resultado<PagoDto>.Fallo(new(TipoError.NoEncontrado, "pago_no_encontrado", "No se encontró el pago de la membresía."))
            : Resultado<PagoDto>.Exito(PagoDto.Desde(pago, Hoy)));
    }

    private DateOnly Hoy => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(reloj.GetUtcNow(), zonaHoraria).DateTime);

    private static Dictionary<string, string[]> ValidarRuta(int clienteId, int membresiaId)
    {
        var errores = new Dictionary<string, string[]>();
        if (clienteId <= 0)
        {
            errores["clienteId"] = ["El ID del cliente debe ser un entero positivo."];
        }

        if (membresiaId <= 0)
        {
            errores["membresiaId"] = ["El ID de la membresía debe ser un entero positivo."];
        }

        return errores;
    }

    private static ErrorAplicacion ErrorDeRegistro(EstadoRegistroPago estado) => estado switch
    {
        EstadoRegistroPago.ClienteInexistente =>
            new(TipoError.NoEncontrado, "cliente_no_encontrado", "No se encontró el cliente."),
        EstadoRegistroPago.ClienteInactivo =>
            new(TipoError.Conflicto, "cliente_inactivo", "El cliente está inactivo."),
        EstadoRegistroPago.MembresiaInexistente =>
            new(TipoError.NoEncontrado, "membresia_no_encontrada", "No se encontró la membresía del cliente."),
        EstadoRegistroPago.MembresiaPagada =>
            new(TipoError.Conflicto, "membresia_ya_pagada", "La membresía ya tiene un pago registrado."),
        EstadoRegistroPago.MembresiaRenovada =>
            new(TipoError.Conflicto, "membresia_ya_renovada", "La membresía ya tiene una renovación registrada."),
        EstadoRegistroPago.PlanInactivo =>
            new(TipoError.Conflicto, "plan_inactivo", "El plan de la membresía está inactivo."),
        EstadoRegistroPago.PeriodoSuperpuesto =>
            new(TipoError.Conflicto, "membresia_periodo_superpuesto", "La renovación se superpone con una membresía del cliente."),
        EstadoRegistroPago.VencimientoFueraDeRango =>
            new(TipoError.Conflicto, "membresia_vencimiento_fuera_de_rango", "La vigencia de la renovación supera el rango de fechas disponible."),
        _ => throw new InvalidOperationException("Estado de registro de pago desconocido.")
    };
}
