using Gymi.Entity;

namespace Gymi.UseCase;

public sealed record DatosAsignacionMembresia(int? PlanId, DateOnly? FechaInicio);

public sealed record MembresiaDto(int Id, int ClienteId, int PlanId, DateOnly FechaInicio, DateOnly FechaVencimiento,
    int SesionesIncluidas, int SesionesDisponibles, string Estado)
{
    public static MembresiaDto Desde(Membresia membresia, DateOnly hoy) =>
        new(membresia.Id, membresia.ClienteId, membresia.PlanId, membresia.FechaInicio, membresia.FechaVencimiento,
            membresia.SesionesIncluidas, membresia.SesionesDisponibles, membresia.EstadoEn(hoy) switch
            {
                EstadoMembresia.Programada => "programada",
                EstadoMembresia.Vigente => "vigente",
                EstadoMembresia.Vencida => "vencida",
                _ => throw new InvalidOperationException("Estado de membresía desconocido.")
            });
}

public sealed record PaginaMembresias(IReadOnlyList<MembresiaDto> Membresias, int Pagina, int TamanoPagina, bool HayMas);

public sealed record AlertaVencimientoDto(int ClienteId, string Nombre, string Apellido, int MembresiaId,
    int PlanId, string NombrePlan, DateOnly FechaVencimiento);

public sealed record PaginaAlertasVencimiento(IReadOnlyList<AlertaVencimientoDto> Alertas, DateOnly FechaReferencia,
    int Pagina, int TamanoPagina, bool HayMas);

public enum EstadoAsignacionMembresia
{
    Creada,
    ClienteInexistente,
    ClienteInactivo,
    PlanInexistente,
    PlanInactivo,
    PlanModificado,
    PeriodoSuperpuesto
}
