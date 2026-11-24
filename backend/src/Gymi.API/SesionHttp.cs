using System.Globalization;
using System.Threading.RateLimiting;
using Gymi.UseCase;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Net.Http.Headers;

namespace Gymi.API;

internal static class SesionHttp
{
    public const string LimiteCredenciales = "credenciales";

    public static void ConfigurarAcceso(this IServiceCollection servicios, TokensJwt tokens)
    {
        servicios.AddSingleton(tokens);
        servicios.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opciones =>
            {
                opciones.MapInboundClaims = false;
                opciones.SaveToken = false;
                opciones.IncludeErrorDetails = false;
                opciones.TokenValidationParameters = tokens.Validacion();
                opciones.Events = new JwtBearerEvents { OnTokenValidated = TokensJwt.ValidarSesionAsync };
            });
        servicios.AddAuthorizationBuilder()
            .AddPolicy(RolesGymi.Personal, politica => politica.RequireRole(RolesGymi.Administrador, RolesGymi.Recepcionista))
            .AddPolicy(RolesGymi.Administrador, politica => politica.RequireRole(RolesGymi.Administrador))
            .AddPolicy(RolesGymi.Cliente, politica => politica.RequireRole(RolesGymi.Cliente));

        servicios.AddRateLimiter(opciones =>
        {
            opciones.AddPolicy(LimiteCredenciales, contexto =>
                RateLimitPartition.GetFixedWindowLimiter(contexto.Connection.RemoteIpAddress?.ToString() ?? "",
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1) }));
            opciones.OnRejected = async (contexto, _) =>
            {
                var http = contexto.HttpContext;
                if (contexto.Lease.TryGetMetadata(MetadataName.RetryAfter, out var espera))
                {
                    http.Response.Headers.RetryAfter = Math.Ceiling(espera.TotalSeconds).ToString(CultureInfo.InvariantCulture);
                }

                await ErroresHttp.DesdeEstado(http, StatusCodes.Status429TooManyRequests).ExecuteAsync(http);
            };
        });
    }

    public static async Task VerificarOrigenAsync(HttpContext contexto, RequestDelegate siguiente)
    {
        var solicitud = contexto.Request;
        if (!HttpMethods.IsGet(solicitud.Method) && !HttpMethods.IsHead(solicitud.Method) && !HttpMethods.IsOptions(solicitud.Method))
        {
            var origen = solicitud.Headers[HeaderNames.Origin];
            var esperado = $"{solicitud.Scheme}://{solicitud.Host}";
            var fetchSite = solicitud.Headers["Sec-Fetch-Site"].ToString();
            if ((origen.Count > 0 && !string.Equals(origen.ToString(), esperado, StringComparison.OrdinalIgnoreCase))
                || fetchSite is "cross-site" or "same-site")
            {
                await ErroresHttp.Crear(contexto, StatusCodes.Status403Forbidden, "origen_no_permitido",
                    "El origen de la solicitud no está permitido.").ExecuteAsync(contexto);
                return;
            }
        }

        await siguiente(contexto);
    }
}
