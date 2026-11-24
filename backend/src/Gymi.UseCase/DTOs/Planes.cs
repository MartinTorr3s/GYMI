using Gymi.Entity;

namespace Gymi.UseCase;

public static class TiposDuracionPlan
{
    public const string Dias = "dias";
    public const string MesCalendario = "mesCalendario";
}

public sealed record DatosPlan(string? Nombre, string? Descripcion, decimal? Precio,
    int? DuracionDias, int? CantidadSesiones, bool? Activo, string? TipoDuracion = null, int? SesionesPorSemana = null);

public sealed record DatosPrevisualizacionPlanMensual(int? SesionesPorSemana);
public sealed record VistaPreviaPlanMensual(string TipoDuracion, int SesionesPorSemana, int CantidadSesiones);

public sealed record PlanDto(int Id, string Nombre, string Descripcion, decimal Precio,
    int? DuracionDias, int CantidadSesiones, bool Activo, string TipoDuracion, Guid Version)
{
    public static PlanDto Desde(PlanMembresia plan) =>
        new(plan.Id, plan.Nombre, plan.Descripcion, plan.Precio, plan.DuracionDias, plan.CantidadSesiones, plan.Activo,
            plan.DuracionDias is null ? TiposDuracionPlan.MesCalendario : TiposDuracionPlan.Dias, plan.Version);
}

public sealed record PaginaPlanes(IReadOnlyList<PlanDto> Planes, int Pagina, int TamanoPagina, bool HayMas);
