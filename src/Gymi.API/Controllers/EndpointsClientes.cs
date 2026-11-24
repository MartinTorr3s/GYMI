using Gymi.UseCase;
using Microsoft.OpenApi;

namespace Gymi.API.Controllers;

internal static class EndpointsClientes
{
    public static void MapearClientes(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/clientes").WithTags("Clientes").RequireAuthorization(RolesGymi.Personal);

        grupo.MapPost("", RegistrarAsync)
            .WithSummary("Registrar un cliente")
            .WithDescription("Enviá el DNI y el teléfono como texto. El DNI admite de 1 a 8 dígitos y el teléfono hasta 15, con + inicial opcional. La respuesta incluye el enlace al cliente en Location.")
            .Produces<ClienteDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status413PayloadTooLarge)
            .ProducesProblem(StatusCodes.Status415UnsupportedMediaType)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        grupo.MapGet("", ListarAsync)
            .AddEndpointFilter(ConsultaHttp.ValidarParametros("pagina", "tamanoPagina", "q"))
            .WithSummary("Listar clientes activos")
            .WithDescription("Filtra todos los clientes activos antes de paginar y conserva el orden por ID. La página predeterminada es 1, con 25 clientes y un máximo de 100.")
            .AddOpenApiOperationTransformer((operacion, _, _) =>
            {
                var busqueda = operacion.Parameters?.OfType<OpenApiParameter>().FirstOrDefault(
                    parametro => parametro.In == ParameterLocation.Query && parametro.Name == "q");
                if (busqueda is not null)
                {
                    busqueda.Description = "Nombre, apellido o nombre completo sin distinguir mayúsculas ni tildes. "
                        + "Para el DNI, enviá todos los dígitos como texto, incluidos los ceros iniciales. "
                        + "Acepta hasta 121 caracteres luego de recortar espacios.";
                }

                return Task.CompletedTask;
            })
            .Produces<PaginaClientes>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        grupo.MapGet("/{id}", ObtenerAsync)
            .WithName("ObtenerCliente")
            .WithSummary("Consultar un cliente activo")
            .Produces<ClienteDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        grupo.MapPut("/{id}", ModificarAsync)
            .DocumentarIfMatch(app.ServiceProvider)
            .WithSummary("Modificar un cliente activo")
            .WithDescription("Enviá todos los campos de la ficha. Se aplican los mismos límites que en el alta.")
            .Produces<ClienteDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status413PayloadTooLarge)
            .ProducesProblem(StatusCodes.Status415UnsupportedMediaType)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        grupo.MapDelete("/{id}", DesactivarAsync)
            .DocumentarIfMatch(app.ServiceProvider)
            .WithSummary("Desactivar un cliente")
            .WithDescription("Conserva el historial y reserva el DNI. El cliente desactivado queda fuera del listado de activos.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    private static async Task<IResult> RegistrarAsync(DatosCliente datos, IClientesController controller,
        HttpContext contexto, CancellationToken ct)
    {
        var resultado = await controller.RegistrarAsync(datos, ct);
        return ErroresHttp.DesdeResultado(resultado, contexto,
            cliente => VersionHttp.ConETag(contexto, cliente.Version, Results.CreatedAtRoute("ObtenerCliente", new { id = cliente.Id }, cliente)));
    }

    private static async Task<IResult> ObtenerAsync(int id, IClientesController controller,
        HttpContext contexto, CancellationToken ct) =>
        ErroresHttp.DesdeResultado(await controller.ObtenerAsync(id, ct), contexto,
            cliente => VersionHttp.ConETag(contexto, cliente.Version, Results.Ok(cliente)));

    private static async Task<IResult> ListarAsync(int? pagina, int? tamanoPagina, string? q,
        IClientesController controller, HttpContext contexto, CancellationToken ct) =>
        ErroresHttp.DesdeResultado(await controller.ListarAsync(pagina ?? 1, tamanoPagina ?? 25, q, ct), contexto,
            clientes => Results.Ok(clientes));

    private static Task<IResult> ModificarAsync(int id, DatosCliente datos,
        IClientesController controller, HttpContext contexto, CancellationToken ct) =>
        VersionHttp.ActualizarAsync(contexto, version => controller.ModificarAsync(id, datos, version, ct),
            ficha => ficha.Version);

    private static Task<IResult> DesactivarAsync(int id,
        IClientesController controller, HttpContext contexto, CancellationToken ct) =>
        VersionHttp.ConVersionAsync(contexto, version => controller.DesactivarAsync(id, version, ct),
            version => VersionHttp.ConETag(contexto, version, Results.NoContent()));
}
