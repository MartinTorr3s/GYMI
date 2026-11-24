namespace Gymi.UseCase;

public enum TipoReporte { ClientesActivos, PagosRegistrados, Asistencias }

public sealed record DatosReporte(int AdministradorId, string? FechaDesde, string? FechaHasta, int? Pagina,
    int? TamanoPagina, Guid? Cursor);

public sealed record PosicionReporte(int Id, long FechaOrden);

public sealed record FiltroReporte(DateOnly? FechaDesde, DateOnly? FechaHasta, int IdMaximo,
    PosicionReporte? DespuesDe, int Cantidad);

public sealed record ContinuacionReporte(Guid IdReporte, TipoReporte Tipo, int AdministradorId,
    DateTimeOffset FechaGeneracion, int Pagina, FiltroReporte Filtro);

public sealed record FilaClienteActivo(int ClienteId, string Nombre, string Apellido, DateOnly FechaAlta);
public sealed record FilaPagoRegistrado(int PagoId, int ClienteId, int MembresiaOrigenId, string Nombre,
    string Apellido, DateOnly FechaPago, decimal Monto, string MedioPago);
public sealed record FilaAsistencia(int AsistenciaId, int ClienteId, string Nombre, string Apellido, int MembresiaId,
    DateTimeOffset FechaHora);

public sealed record ReporteDto<T>(Guid IdReporte, string TipoReporte, DateOnly? FechaDesde, DateOnly? FechaHasta,
    DateTimeOffset FechaGeneracion, IReadOnlyList<T> Filas, int Pagina, int TamanoPagina, bool HayMas, Guid? CursorSiguiente);
