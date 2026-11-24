using Gymi.UseCase;

namespace Gymi.API.Controllers;

internal static class EndpointsPlanes
{
    public static void MapearPlanes(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/planes").WithTags("Planes").RequireAuthorization(RolesGymi.Personal);

        grupo.MapPost("/previsualizar", PrevisualizarMensual)
            .RequireAuthorization(RolesGymi.Administrador)
            .WithSummary("Sugerir un cupo para un plan mensual")
            .AddOpenApiOperationTransformer(PlanesOpenApi.EjemploDePrevisualizacion)
            .WithDescription("Ingresá las sesiones por semana para obtener una sugerencia. Confirmá o ajustá el cupo al guardar el plan.")
            .Produces<VistaPreviaPlanMensual>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status413PayloadTooLarge)
            .ProducesProblem(StatusCodes.Status415UnsupportedMediaType);

        grupo.MapPost("", RegistrarAsync)
            .RequireAuthorization(RolesGymi.Administrador)
            .WithSummary("Registrar un plan de membresía")
            .AddOpenApiOperationTransformer(PlanesOpenApi.EjemplosDeAltaYEdicion)
            .WithDescription("Enviá la cantidad de sesiones incluidas. Para un mes calendario, usá tipoDuracion con valor mesCalendario. Para un plan por días, indicá también duracionDias.")
            .Produces<PlanDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status413PayloadTooLarge)
            .ProducesProblem(StatusCodes.Status415UnsupportedMediaType)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        grupo.MapGet("", ListarAsync)
            .AddEndpointFilter(ConsultaHttp.ValidarParametros("activo", "pagina", "tamanoPagina"))
            .WithSummary("Listar planes de membresía")
            .WithDescription("Incluye planes activos e inactivos. Usá el filtro activo para consultar un estado. La página predeterminada es 1, con 25 resultados y un máximo de 100.")
            .Produces<PaginaPlanes>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        grupo.MapGet("/{id}", ObtenerAsync)
            .WithName("ObtenerPlan")
            .WithSummary("Consultar un plan por ID")
            .Produces<PlanDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        grupo.MapPut("/{id}", ModificarAsync)
            .DocumentarIfMatch(app.ServiceProvider)
            .RequireAuthorization(RolesGymi.Administrador)
            .WithSummary("Modificar un plan de membresía")
            .AddOpenApiOperationTransformer(PlanesOpenApi.EjemplosDeAltaYEdicion)
            .WithDescription("Reemplaza los datos del plan. La duración y el cupo siguen las mismas reglas que el alta. Enviá activo en true para reactivar un plan.")
            .Produces<PlanDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status413PayloadTooLarge)
            .ProducesProblem(StatusCodes.Status415UnsupportedMediaType)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        grupo.MapDelete("/{id}", DesactivarAsync)
            .DocumentarIfMatch(app.ServiceProvider)
            .RequireAuthorization(RolesGymi.Administrador)
            .WithSummary("Desactivar un plan de membresía")
            .WithDescription("Desactiva el plan y conserva los datos y las membresías asociadas.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    private static IResult PrevisualizarMensual(DatosPrevisualizacionPlanMensual datos,
        IPlanesController controller, HttpContext contexto) =>
        ErroresHttp.DesdeResultado(controller.PrevisualizarMensual(datos), contexto, vista => Results.Ok(vista));

    private static async Task<IResult> RegistrarAsync(DatosPlan datos, IPlanesController controller,
        HttpContext contexto, CancellationToken ct)
    {
        var resultado = await controller.RegistrarAsync(datos, ct);
        return ErroresHttp.DesdeResultado(resultado, contexto,
            plan => VersionHttp.ConETag(contexto, plan.Version, Results.CreatedAtRoute("ObtenerPlan", new { id = plan.Id }, plan)));
    }

    private static async Task<IResult> ObtenerAsync(int id, IPlanesController controller,
        HttpContext contexto, CancellationToken ct) =>
        ErroresHttp.DesdeResultado(await controller.ObtenerAsync(id, ct), contexto,
            plan => VersionHttp.ConETag(contexto, plan.Version, Results.Ok(plan)));

    private static async Task<IResult> ListarAsync(int? pagina, int? tamanoPagina, bool? activo,
        IPlanesController controller, HttpContext contexto, CancellationToken ct) =>
        ErroresHttp.DesdeResultado(await controller.ListarAsync(pagina ?? 1, tamanoPagina ?? 25, activo, ct),
            contexto, planes => Results.Ok(planes));

    private static Task<IResult> ModificarAsync(int id, DatosPlan datos,
        IPlanesController controller, HttpContext contexto, CancellationToken ct) =>
        VersionHttp.ActualizarAsync(contexto, version => controller.ModificarAsync(id, datos, version, ct),
            ficha => ficha.Version);

    private static Task<IResult> DesactivarAsync(int id,
        IPlanesController controller, HttpContext contexto, CancellationToken ct) =>
        VersionHttp.ConVersionAsync(contexto, version => controller.DesactivarAsync(id, version, ct),
            version => VersionHttp.ConETag(contexto, version, Results.NoContent()));
}
