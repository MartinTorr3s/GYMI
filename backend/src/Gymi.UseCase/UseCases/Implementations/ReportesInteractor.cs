using System.Globalization;
using Gymi.UseCase;

namespace Gymi.UseCase.UseCases.Implementations;

public sealed class ReportesInteractor(IReporteRepository repository, ICursoresReporte cursores,
    IPresentador<ReporteDto<FilaClienteActivo>> clientes, IPresentador<ReporteDto<FilaPagoRegistrado>> pagos,
    IPresentador<ReporteDto<FilaAsistencia>> asistencias, TimeProvider reloj, TimeZoneInfo zonaHoraria) : IReportesEntrada
{
    public Task ClientesActivosAsync(DatosReporte datos, CancellationToken ct) =>
        GenerarAsync(datos, TipoReporte.ClientesActivos, "clientes_activos", clientes, repository.ListarClientesActivosAsync,
            fila => new(fila.ClienteId, fila.FechaAlta.DayNumber), ct);

    public Task PagosRegistradosAsync(DatosReporte datos, CancellationToken ct) =>
        GenerarAsync(datos, TipoReporte.PagosRegistrados, "pagos_registrados", pagos, repository.ListarPagosAsync,
            fila => new(fila.PagoId, fila.FechaPago.DayNumber), ct);

    public Task AsistenciasAsync(DatosReporte datos, CancellationToken ct) =>
        GenerarAsync(datos, TipoReporte.Asistencias, "asistencias", asistencias, ListarAsistenciasAsync,
            fila => new(fila.AsistenciaId, fila.FechaHora.UtcTicks), ct);

    private async Task GenerarAsync<T>(DatosReporte datos, TipoReporte tipo, string nombre,
        IPresentador<ReporteDto<T>> salida, Func<FiltroReporte, CancellationToken, Task<IReadOnlyList<T>>> consultar,
        Func<T, PosicionReporte> posicion, CancellationToken ct)
    {
        var validacion = await PrepararAsync(datos, tipo, ct);
        if (validacion.Error is { } error)
        {
            salida.Presentar(Resultado<ReporteDto<T>>.Fallo(error));
            return;
        }

        var reporte = validacion.Valor!;
        var filtro = reporte.Filtro;
        var tamano = filtro.Cantidad - 1;
        var filas = await consultar(filtro, ct);
        var hayMas = filas.Count > tamano;
        Guid? siguiente = hayMas ? cursores.Guardar(reporte with
        {
            Pagina = reporte.Pagina + 1,
            Filtro = filtro with { DespuesDe = posicion(filas[tamano - 1]) }
        }) : null;

        salida.Presentar(Resultado<ReporteDto<T>>.Exito(new(reporte.IdReporte, nombre, filtro.FechaDesde, filtro.FechaHasta,
            reporte.FechaGeneracion, filas.Take(tamano).ToArray(), reporte.Pagina, tamano, hayMas, siguiente)));
    }

    private async Task<Resultado<ContinuacionReporte>> PrepararAsync(DatosReporte datos, TipoReporte tipo, CancellationToken ct)
    {
        if (datos.Cursor is { } cursor)
        {
            if (cursor == Guid.Empty || datos.FechaDesde is not null || datos.FechaHasta is not null ||
                datos.Pagina is not null || datos.TamanoPagina is not null)
            {
                return Resultado<ContinuacionReporte>.Fallo(ValidacionEntrada.Error(new()
                {
                    ["cursor"] = ["Enviá solo el cursor devuelto en la página anterior."]
                }));
            }

            var continuacion = cursores.Buscar(cursor);
            return continuacion is not null && continuacion.Tipo == tipo && continuacion.AdministradorId == datos.AdministradorId
                ? Resultado<ContinuacionReporte>.Exito(continuacion)
                : Resultado<ContinuacionReporte>.Fallo(new(TipoError.NoEncontrado, "cursor_no_disponible",
                    "Volvé a iniciar el reporte para continuar."));
        }

        var tamano = datos.TamanoPagina ?? 25;
        var errores = ValidacionEntrada.CalcularOffset(1, tamano).Error?.Campos ?? new Dictionary<string, string[]>();
        if (datos.Pagina is not (null or 1))
        {
            errores["pagina"] = ["Iniciá en la página 1. Para continuar usá el cursor devuelto por la API."];
        }

        var desde = LeerFecha(datos.FechaDesde, "fechaDesde", errores);
        var hasta = LeerFecha(datos.FechaHasta, "fechaHasta", errores);
        if (datos.FechaDesde is null && datos.FechaHasta is not null)
        {
            errores["fechaDesde"] = ["Indicá las dos fechas para filtrar por período."];
        }
        if (datos.FechaHasta is null && datos.FechaDesde is not null)
        {
            errores["fechaHasta"] = ["Indicá las dos fechas para filtrar por período."];
        }
        if (desde > hasta)
        {
            errores["fechaHasta"] = ["La fecha hasta debe ser igual o posterior a la fecha desde."];
        }
        if (errores.Count > 0)
        {
            return Resultado<ContinuacionReporte>.Fallo(ValidacionEntrada.Error(errores));
        }

        var idMaximo = await repository.LeerIdMaximoAsync(tipo, ct);
        var filtro = new FiltroReporte(desde, hasta, idMaximo, null, tamano + 1);
        return Resultado<ContinuacionReporte>.Exito(new(Guid.NewGuid(), tipo, datos.AdministradorId, reloj.GetUtcNow(), 1, filtro));
    }

    private Task<IReadOnlyList<FilaAsistencia>> ListarAsistenciasAsync(FiltroReporte filtro, CancellationToken ct)
    {
        DateTimeOffset? desde = filtro.FechaDesde is { } inicio && inicio != DateOnly.MinValue
            ? InicioDiaUtc(inicio) : null;
        DateTimeOffset? hastaExclusiva = filtro.FechaHasta is { } fin && fin != DateOnly.MaxValue
            ? InicioDiaUtc(fin.AddDays(1)) : null;

        return repository.ListarAsistenciasAsync(filtro, desde, hastaExclusiva, ct);
    }

    private DateTimeOffset InicioDiaUtc(DateOnly fecha)
    {
        var inicio = fecha.ToDateTime(TimeOnly.MinValue);
        // Algunos cambios históricos de hora local saltaron la medianoche.
        while (zonaHoraria.IsInvalidTime(inicio))
        {
            inicio = inicio.AddMinutes(1);
        }

        if (zonaHoraria.IsAmbiguousTime(inicio))
        {
            var primerOffset = zonaHoraria.GetAmbiguousTimeOffsets(inicio).Max();
            return new DateTimeOffset(inicio, primerOffset).ToUniversalTime();
        }

        return TimeZoneInfo.ConvertTimeToUtc(inicio, zonaHoraria);
    }

    private static DateOnly? LeerFecha(string? entrada, string campo, Dictionary<string, string[]> errores)
    {
        if (entrada is null)
        {
            return null;
        }

        if (DateOnly.TryParseExact(entrada, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha))
        {
            return fecha;
        }

        errores[campo] = ["Ingresá una fecha válida con formato AAAA-MM-DD."];
        return null;
    }
}
