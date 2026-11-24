using System.Globalization;
using System.Security.Claims;
using Gymi.UseCase;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Gymi.API;

internal sealed record InicioSesionJwtDto(int Id, string Nombre, string Apellido, string Rol,
    string AccessToken, string TokenType, DateTimeOffset VenceEn);

internal sealed class TokensJwt
{
    private const string TipoAcceso = "at+jwt";
    private readonly JsonWebTokenHandler _emisor = new();
    private readonly SigningCredentials _firma;
    private readonly string _emisorEsperado;
    private readonly string _audiencia;

    public TokensJwt(IConfiguration configuracion, IWebHostEnvironment entorno)
    {
        _emisorEsperado = configuracion["Jwt:Emisor"] ?? "gymi";
        _audiencia = configuracion["Jwt:Audiencia"] ?? "gymi-api";
        if (string.IsNullOrWhiteSpace(_emisorEsperado) || string.IsNullOrWhiteSpace(_audiencia))
        {
            throw new InvalidOperationException("Jwt:Emisor y Jwt:Audiencia deben tener un valor.");
        }

        _firma = new SigningCredentials(new SymmetricSecurityKey(ClaveJwt.Cargar(configuracion, entorno)),
            SecurityAlgorithms.HmacSha256);
    }

    public InicioSesionJwtDto Emitir(SesionAbierta sesion)
    {
        var usuario = sesion.Usuario;
        var token = _emisor.CreateToken(new SecurityTokenDescriptor
        {
            TokenType = TipoAcceso,
            Issuer = _emisorEsperado,
            Audience = _audiencia,
            IssuedAt = sesion.EmitidaEn.UtcDateTime,
            NotBefore = sesion.EmitidaEn.UtcDateTime,
            Expires = sesion.VenceEn.UtcDateTime,
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = usuario.Id.ToString(CultureInfo.InvariantCulture),
                [JwtRegisteredClaimNames.Jti] = sesion.IdSesion,
                ["role"] = usuario.Rol
            },
            SigningCredentials = _firma
        });

        return new(usuario.Id, usuario.Nombre, usuario.Apellido, usuario.Rol, token,
            JwtBearerDefaults.AuthenticationScheme, sesion.VenceEn);
    }

    public TokenValidationParameters Validacion() => new()
    {
        ValidateIssuer = true,
        ValidIssuer = _emisorEsperado,
        ValidateAudience = true,
        ValidAudience = _audiencia,
        IgnoreTrailingSlashWhenValidatingAudience = false,
        RequireSignedTokens = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = _firma.Key,
        ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
        ValidTypes = [TipoAcceso],
        ValidateLifetime = true,
        RequireExpirationTime = true,
        ClockSkew = TimeSpan.Zero
    };

    public static async Task ValidarSesionAsync(TokenValidatedContext contexto)
    {
        var principal = contexto.Principal;
        var idSesion = principal?.FindFirstValue(JwtRegisteredClaimNames.Jti);
        if (idSesion is null || !int.TryParse(principal?.FindFirstValue(JwtRegisteredClaimNames.Sub),
            NumberStyles.None, CultureInfo.InvariantCulture, out var usuarioId) || usuarioId <= 0)
        {
            contexto.Fail("Sesión inválida.");
            return;
        }

        var control = contexto.HttpContext.RequestServices.GetRequiredService<IControlDeSesion>();
        var usuario = await control.ValidarAsync(idSesion, contexto.HttpContext.RequestAborted);
        if (usuario is null || usuario.Id != usuarioId)
        {
            contexto.Fail("Sesión inválida.");
            return;
        }

        var identidad = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString(CultureInfo.InvariantCulture)),
            new Claim(ClaimTypes.Role, usuario.Rol),
            new Claim(JwtRegisteredClaimNames.Jti, idSesion)
        ], JwtBearerDefaults.AuthenticationScheme);
        contexto.Principal = new ClaimsPrincipal(identidad);
    }
}
