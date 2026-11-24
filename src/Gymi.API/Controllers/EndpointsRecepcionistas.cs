using Gymi.UseCase;

namespace Gymi.API.Controllers;

internal static class EndpointsRecepcionistas
{
    public static void MapearRecepcionistas(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/recepcionistas").WithTags("Recepcionistas").RequireAuthorization(RolesGymi.Administrador);

        grupo.MapPost("", RegistrarAsync)
            .WithSummary("Dar de alta a un recepcionista")
            .WithDescription("Enviá nombre, apellido y correo. La ficha queda activa y el acceso web queda pendiente.")
            .Produces<RecepcionistaDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        grupo.MapGet("", ListarAsync)
            .AddEndpointFilter(ConsultaHttp.ValidarParametros("activo", "pagina", "tamanoPagina"))
            .WithSummary("Listar recepcionistas")
            .WithDescription("La respuesta es paginada. Usá activo para filtrar por estado.")
            .Produces<PaginaRecepcionistas>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        grupo.MapGet("/{id}", ObtenerAsync)
            .WithName("ObtenerRecepcionista")
            .WithSummary("Consultar la ficha de un recepcionista")
            .Produces<RecepcionistaDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        grupo.MapPut("/{id}", ModificarAsync)
            .DocumentarIfMatch(app.ServiceProvider)
            .WithSummary("Modificar nombre, apellido y correo del recepcionista")
            .Produces<RecepcionistaDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        grupo.MapDelete("/{id}", DesactivarAsync)
            .DocumentarIfMatch(app.ServiceProvider)
            .WithSummary("Desactivar un recepcionista y revocar su acceso")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        grupo.MapPost("/{id}/activacion", InvitarAsync)
            .WithSummary("Emitir una invitación de acceso para el recepcionista")
            .WithDescription("El recepcionista usa el código de 24 horas en POST /acceso/activar para elegir su contraseña.")
            .Produces<InvitacionActivacion>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> RegistrarAsync(DatosRecepcionista datos, IRecepcionistasController controller,
        HttpContext contexto, CancellationToken ct) =>
        ErroresHttp.DesdeResultado(await controller.RegistrarAsync(datos, ct), contexto,
            ficha => VersionHttp.ConETag(contexto, ficha.Version, Results.CreatedAtRoute("ObtenerRecepcionista", new { id = ficha.Id }, ficha)));

    private static async Task<IResult> ObtenerAsync(int id, IRecepcionistasController controller,
        HttpContext contexto, CancellationToken ct) =>
        ErroresHttp.DesdeResultado(await controller.ObtenerAsync(id, ct), contexto,
            ficha => VersionHttp.ConETag(contexto, ficha.Version, Results.Ok(ficha)));

    private static async Task<IResult> ListarAsync(bool? activo, int? pagina, int? tamanoPagina,
        IRecepcionistasController controller, HttpContext contexto, CancellationToken ct) =>
        ErroresHttp.DesdeResultado(await controller.ListarAsync(activo, pagina ?? 1, tamanoPagina ?? 25, ct),
            contexto, listado => Results.Ok(listado));

    private static Task<IResult> ModificarAsync(int id, DatosRecepcionista datos,
        IRecepcionistasController controller, HttpContext contexto, CancellationToken ct) =>
        VersionHttp.ActualizarAsync(contexto, version => controller.ModificarAsync(id, datos, version, ct),
            ficha => ficha.Version);

    private static Task<IResult> DesactivarAsync(int id,
        IRecepcionistasController controller, HttpContext contexto, CancellationToken ct) =>
        VersionHttp.ConVersionAsync(contexto, version => controller.DesactivarAsync(id, version, ct),
            version => VersionHttp.ConETag(contexto, version, Results.NoContent()));

    private static async Task<IResult> InvitarAsync(int id, IAccesoController controller, HttpContext contexto,
        CancellationToken ct) =>
        ErroresHttp.DesdeResultado(await controller.InvitarAsync(id, TipoInvitacion.Recepcionista, ct), contexto,
            invitacion => Results.Ok(invitacion));
}
