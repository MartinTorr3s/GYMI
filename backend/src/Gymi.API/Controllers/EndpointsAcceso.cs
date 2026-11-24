using System.Globalization;
using System.Security.Claims;
using Gymi.UseCase;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Gymi.API.Controllers;

internal static class EndpointsAcceso
{
    public static void MapearAcceso(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/acceso").WithTags("Acceso");
        grupo.MapPost("/iniciar-sesion", IniciarSesionAsync)
            .AllowAnonymous()
            .RequireRateLimiting(SesionHttp.LimiteCredenciales)
            .WithSummary("Iniciar sesión con correo y contraseña")
            .WithDescription("Copiá accessToken de la respuesta y pegalo en Authorize antes de probar las rutas protegidas.")
            .Produces<InicioSesionJwtDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        grupo.MapPost("/cerrar-sesion", CerrarSesionAsync)
            .RequireAuthorization()
            .WithSummary("Cerrar la sesión actual")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        grupo.MapPost("/activar", ActivarAsync)
            .AllowAnonymous()
            .RequireRateLimiting(SesionHttp.LimiteCredenciales)
            .WithSummary("Configurar la contraseña mediante una invitación de acceso")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        app.MapPost("/clientes/{clienteId}/activacion", InvitarAsync)
            .WithTags("Acceso")
            .RequireAuthorization(RolesGymi.Personal)
            .WithSummary("Emitir una invitación de acceso para un cliente")
            .WithDescription("Entregá el código al titular después de verificar su identidad. La invitación dura 24 horas.")
            .Produces<InvitacionActivacion>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        var cuenta = app.MapGroup("/mi-membresia").WithTags("Mi membresía").RequireAuthorization(RolesGymi.Cliente);
        cuenta.MapGet("", MiMembresiaAsync)
            .WithSummary("Consultar la ficha y el período de la cuenta autenticada")
            .Produces<MiMembresiaDto>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        cuenta.MapGet("/pagos", MisPagosAsync)
            .WithSummary("Listar los pagos de la cuenta autenticada")
            .Produces<PaginaPagosCuenta>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> IniciarSesionAsync(DatosInicioSesion datos, IAccesoController controller,
        TokensJwt tokens, HttpContext contexto, CancellationToken ct) =>
        ErroresHttp.DesdeResultado(await controller.IniciarSesionAsync(datos, ct), contexto,
            sesion => Results.Ok(tokens.Emitir(sesion)));

    private static async Task<IResult> CerrarSesionAsync(IControlDeSesion control, HttpContext contexto, CancellationToken ct)
    {
        await control.CerrarAsync(contexto.User.FindFirstValue(JwtRegisteredClaimNames.Jti), ct);
        return Results.NoContent();
    }

    private static async Task<IResult> InvitarAsync(int clienteId, IAccesoController controller,
        HttpContext contexto, CancellationToken ct) =>
        ErroresHttp.DesdeResultado(await controller.InvitarAsync(clienteId, TipoInvitacion.Cliente, ct), contexto, invitacion => Results.Ok(invitacion));

    private static async Task<IResult> ActivarAsync(DatosActivacion datos, IAccesoController controller,
        HttpContext contexto, CancellationToken ct) =>
        ErroresHttp.DesdeResultado(await controller.ActivarAsync(datos, ct), contexto, _ => Results.NoContent());

    private static async Task<IResult> MiMembresiaAsync(IAccesoController controller, HttpContext contexto, CancellationToken ct) =>
        ObtenerClienteId(contexto, out var clienteId)
            ? ErroresHttp.DesdeResultado(await controller.MiMembresiaAsync(clienteId, ct), contexto, ficha => Results.Ok(ficha))
            : ErroresHttp.DesdeEstado(contexto, StatusCodes.Status401Unauthorized);

    private static async Task<IResult> MisPagosAsync(int? pagina, int? tamanoPagina, IAccesoController controller,
        HttpContext contexto, CancellationToken ct) =>
        ObtenerClienteId(contexto, out var clienteId)
            ? ErroresHttp.DesdeResultado(await controller.MisPagosAsync(clienteId, pagina ?? 1, tamanoPagina ?? 25, ct),
                contexto, pagos => Results.Ok(pagos))
            : ErroresHttp.DesdeEstado(contexto, StatusCodes.Status401Unauthorized);

    private static bool ObtenerClienteId(HttpContext contexto, out int clienteId) =>
        int.TryParse(contexto.User.FindFirstValue(ClaimTypes.NameIdentifier), NumberStyles.None,
            CultureInfo.InvariantCulture, out clienteId) && clienteId > 0;
}
