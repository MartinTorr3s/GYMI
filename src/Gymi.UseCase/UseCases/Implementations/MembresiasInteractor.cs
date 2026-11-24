using Gymi.UseCase;
using Gymi.Entity;

namespace Gymi.UseCase.UseCases.Implementations;

public sealed class MembresiasInteractor(IMembresiaRepository repository, IClienteRepository clientes,
    IPlanRepository planes, IPresentador<MembresiaDto> ficha, IPresentador<PaginaMembresias> paginaSalida,
    IPresentador<PaginaAlertasVencimiento> alertasSalida, TimeProvider reloj, TimeZoneInfo zonaHoraria) : IMembresiasEntrada
{
    private static readonly ErrorAplicacion ClienteInexistente =
        new(TipoError.NoEncontrado, "cliente_no_encontrado", "No se encontró el cliente.");
    private static readonly ErrorAplicacion PlanInexistente =
        new(TipoError.NoEncontrado, "plan_no_encontrado", "No se encontró el plan.");
    private static readonly ErrorAplicacion PlanInactivo =
        new(TipoError.Conflicto, "plan_inactivo", "El plan está inactivo.");
    private static readonly ErrorAplicacion MembresiaInexistente =
        new(TipoError.NoEncontrado, "membresia_no_encontrada", "No se encontró la membresía del cliente.");

    public async Task AsignarAsync(int clienteId, DatosAsignacionMembresia datos, CancellationToken ct)
    {
        var errores = new Dictionary<string, string[]>();
        ValidarClienteId(clienteId, errores);
        if (datos.PlanId is null or <= 0)
        {
            errores["planId"] = ["El ID del plan debe ser un entero positivo."];
        }

        if (datos.FechaInicio is null)
        {
            errores["fechaInicio"] = ["Ingresá la fecha de inicio."];
        }

        if (errores.Count > 0)
        {
            ficha.Presentar(Resultado<MembresiaDto>.Fallo(ValidacionEntrada.Error(errores)));
            return;
        }

        var plan = await planes.BuscarAsync(datos.PlanId!.Value, ct);
        if (plan is not { Activo: true })
        {
            ficha.Presentar(Resultado<MembresiaDto>.Fallo(plan is null ? PlanInexistente : PlanInactivo));
            return;
        }

        var inicio = datos.FechaInicio!.Value;
        if (Membresia.CalcularVencimiento(inicio, plan) is not { } vencimiento)
        {
            errores["fechaInicio"] = ["La vigencia del plan supera el rango de fechas disponible."];
            ficha.Presentar(Resultado<MembresiaDto>.Fallo(ValidacionEntrada.Error(errores)));
            return;
        }

        var membresia = new Membresia(clienteId, plan, inicio, vencimiento);
        var estado = await repository.GuardarAsignacionAsync(membresia, plan.Version, ct);
        ficha.Presentar(estado == EstadoAsignacionMembresia.Creada
            ? Resultado<MembresiaDto>.Exito(MembresiaDto.Desde(membresia, Hoy))
            : Resultado<MembresiaDto>.Fallo(ErrorDeAsignacion(estado)));
    }

    public async Task ObtenerAsync(int clienteId, int id, CancellationToken ct)
    {
        var errores = new Dictionary<string, string[]>();
        ValidarClienteId(clienteId, errores);
        if (id <= 0)
        {
            errores["id"] = ["El ID de la membresía debe ser un entero positivo."];
        }

        if (errores.Count > 0)
        {
            ficha.Presentar(Resultado<MembresiaDto>.Fallo(ValidacionEntrada.Error(errores)));
            return;
        }

        var membresia = await repository.BuscarAsync(clienteId, id, ct);
        ficha.Presentar(membresia is null
            ? Resultado<MembresiaDto>.Fallo(MembresiaInexistente)
            : Resultado<MembresiaDto>.Exito(MembresiaDto.Desde(membresia, Hoy)));
    }

    public async Task ListarAsync(int clienteId, int pagina, int tamanoPagina, CancellationToken ct)
    {
        var errores = new Dictionary<string, string[]>();
        ValidarClienteId(clienteId, errores);
        var offset = ValidacionEntrada.CalcularOffset(pagina, tamanoPagina);
        if (errores.Count > 0 || offset.Error is not null)
        {
            paginaSalida.Presentar(Resultado<PaginaMembresias>.Fallo(
                errores.Count > 0 ? ValidacionEntrada.Error(errores) : offset.Error!));
            return;
        }

        if (await clientes.BuscarAsync(clienteId, ct) is null)
        {
            paginaSalida.Presentar(Resultado<PaginaMembresias>.Fallo(ClienteInexistente));
            return;
        }

        var registros = await repository.ListarAsync(clienteId, offset.Valor, tamanoPagina + 1, ct);
        var hoy = Hoy;
        var resultados = registros.Take(tamanoPagina).Select(m => MembresiaDto.Desde(m, hoy)).ToArray();
        paginaSalida.Presentar(Resultado<PaginaMembresias>.Exito(
            new(resultados, pagina, tamanoPagina, registros.Count > tamanoPagina)));
    }

    public async Task ListarVencidasAsync(int pagina, int tamanoPagina, CancellationToken ct)
    {
        var offset = ValidacionEntrada.CalcularOffset(pagina, tamanoPagina);
        if (offset.Error is { } error)
        {
            alertasSalida.Presentar(Resultado<PaginaAlertasVencimiento>.Fallo(error));
            return;
        }

        var hoy = Hoy;
        var registros = await repository.ListarVencidasAsync(hoy, offset.Valor, tamanoPagina + 1, ct);
        alertasSalida.Presentar(Resultado<PaginaAlertasVencimiento>.Exito(
            new(registros.Take(tamanoPagina).ToArray(), hoy, pagina, tamanoPagina, registros.Count > tamanoPagina)));
    }

    private DateOnly Hoy => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(reloj.GetUtcNow(), zonaHoraria).Date);

    private static void ValidarClienteId(int clienteId, Dictionary<string, string[]> errores)
    {
        if (clienteId <= 0)
        {
            errores["clienteId"] = ["El ID del cliente debe ser un entero positivo."];
        }
    }

    private static ErrorAplicacion ErrorDeAsignacion(EstadoAsignacionMembresia estado) => estado switch
    {
        EstadoAsignacionMembresia.ClienteInexistente => ClienteInexistente,
        EstadoAsignacionMembresia.ClienteInactivo =>
            new(TipoError.Conflicto, "cliente_inactivo", "El cliente está inactivo."),
        EstadoAsignacionMembresia.PlanInexistente => PlanInexistente,
        EstadoAsignacionMembresia.PlanInactivo => PlanInactivo,
        EstadoAsignacionMembresia.PlanModificado =>
            new(TipoError.Conflicto, "plan_modificado", "El plan cambió durante la asignación. Volvé a seleccionarlo."),
        EstadoAsignacionMembresia.PeriodoSuperpuesto =>
            new(TipoError.Conflicto, "membresia_periodo_superpuesto",
                "El cliente ya tiene una membresía que se superpone con el período indicado."),
        _ => throw new ArgumentOutOfRangeException(nameof(estado))
    };
}
