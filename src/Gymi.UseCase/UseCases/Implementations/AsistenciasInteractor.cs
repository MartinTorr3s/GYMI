using Gymi.UseCase;

namespace Gymi.UseCase.UseCases.Implementations;

public sealed class AsistenciasInteractor(IAsistenciaRepository repository, IPresentador<ConsultaAsistenciaDto> consultaSalida,
    IPresentador<ConfirmacionAsistencia> registroSalida, IPresentador<AsistenciaDto> detalleSalida,
    TimeProvider reloj, TimeZoneInfo zonaHoraria) : IAsistenciasEntrada
{
    public async Task ConsultarClienteAsync(DatosConsultaAsistencia datos, CancellationToken ct)
    {
        var errores = new Dictionary<string, string[]>();
        var dni = ClienteEspecificacion.ValidarDni(datos.Dni, errores);
        if (errores.Count > 0)
        {
            consultaSalida.Presentar(Resultado<ConsultaAsistenciaDto>.Fallo(ValidacionEntrada.Error(errores)));
            return;
        }

        var hoy = FechaLocal(reloj.GetUtcNow());
        var consulta = await repository.ConsultarClienteAsync(dni, hoy, ct);
        if (consulta is null)
        {
            consultaSalida.Presentar(Resultado<ConsultaAsistenciaDto>.Fallo(ErrorDeAcceso(EstadoAcceso.ClienteInexistente)));
            return;
        }

        var estado = consulta.Evaluar(hoy);
        var habilitado = estado == EstadoAcceso.Habilitado;
        var cliente = consulta.Cliente;
        var membresia = consulta.Membresia is { } periodo ? MembresiaDto.Desde(periodo, hoy) : null;
        consultaSalida.Presentar(Resultado<ConsultaAsistenciaDto>.Exito(
            new(cliente.Id, cliente.Nombre, cliente.Apellido, membresia, habilitado, habilitado ? null : ErrorDeAcceso(estado).Codigo)));
    }

    public async Task RegistrarAsync(int clienteId, DatosRegistroAsistencia datos, CancellationToken ct)
    {
        var errores = ValidarClienteId(clienteId);
        if (datos.OperacionId is not { } operacionId || operacionId == Guid.Empty)
        {
            errores["operacionId"] = ["Enviá un UUID de operación válido."];
        }

        if (errores.Count > 0)
        {
            registroSalida.Presentar(Resultado<ConfirmacionAsistencia>.Fallo(ValidacionEntrada.Error(errores)));
            return;
        }

        var ahora = reloj.GetUtcNow();
        var registro = await repository.RegistrarAsync(clienteId, datos.OperacionId!.Value, ahora, FechaLocal(ahora), ct);
        registroSalida.Presentar(registro.Asistencia is { } asistencia
            ? Resultado<ConfirmacionAsistencia>.Exito(new(AsistenciaDto.Desde(asistencia), registro.EsReintento))
            : Resultado<ConfirmacionAsistencia>.Fallo(ErrorDeAcceso(registro.Estado)));
    }

    public async Task ObtenerAsync(int clienteId, int asistenciaId, CancellationToken ct)
    {
        var errores = ValidarClienteId(clienteId);
        if (asistenciaId <= 0)
        {
            errores["asistenciaId"] = ["El ID de la asistencia debe ser un entero positivo."];
        }

        if (errores.Count > 0)
        {
            detalleSalida.Presentar(Resultado<AsistenciaDto>.Fallo(ValidacionEntrada.Error(errores)));
            return;
        }

        var asistencia = await repository.BuscarAsync(clienteId, asistenciaId, ct);
        detalleSalida.Presentar(asistencia is null
            ? Resultado<AsistenciaDto>.Fallo(new(TipoError.NoEncontrado, "asistencia_no_encontrada", "No se encontró la asistencia del cliente."))
            : Resultado<AsistenciaDto>.Exito(AsistenciaDto.Desde(asistencia)));
    }

    private DateOnly FechaLocal(DateTimeOffset fecha) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(fecha, zonaHoraria).DateTime);

    private static Dictionary<string, string[]> ValidarClienteId(int clienteId) => clienteId > 0
        ? new()
        : new() { ["clienteId"] = ["El ID del cliente debe ser un entero positivo."] };

    private static ErrorAplicacion ErrorDeAcceso(EstadoAcceso estado) => estado switch
    {
        EstadoAcceso.ClienteInexistente => new(TipoError.NoEncontrado, "cliente_no_encontrado", "No se encontró el cliente."),
        EstadoAcceso.ClienteInactivo => new(TipoError.Conflicto, "cliente_inactivo", "El cliente está inactivo."),
        EstadoAcceso.MembresiaNoAsignada => new(TipoError.Conflicto, "membresia_no_asignada", "El cliente aún no tiene una membresía asignada."),
        EstadoAcceso.MembresiaProgramada => new(TipoError.Conflicto, "membresia_programada", "La membresía aún no comenzó."),
        EstadoAcceso.MembresiaVencida => new(TipoError.Conflicto, "membresia_vencida", "La membresía está vencida."),
        EstadoAcceso.SesionesAgotadas => new(TipoError.Conflicto, "sesiones_agotadas", "La membresía se quedó sin sesiones disponibles."),
        EstadoAcceso.OperacionAsignada => new(TipoError.Conflicto, "operacion_ya_utilizada", "El identificador de operación ya está asociado a otra asistencia."),
        _ => throw new InvalidOperationException("Estado de acceso desconocido.")
    };
}
