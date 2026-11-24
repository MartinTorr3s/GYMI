using System.Security.Cryptography;
using System.Text;

namespace Gymi.UseCase.UseCases.Implementations;

internal static class TokensAcceso
{
    public static string Crear() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    public static bool EsValido(string? token) => token is { Length: 64 } && token.All(Uri.IsHexDigit);

    public static string Huella(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.ASCII.GetBytes(token.ToUpperInvariant())));
}
