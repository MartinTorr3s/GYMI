using Gymi.UseCase;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Gymi.Repositories.Security;

public sealed class ProteccionContrasenas : IProteccionContrasenas
{
    private readonly object _usuario = new();
    private readonly PasswordHasher<object> _hasher = new(Options.Create(
        new PasswordHasherOptions { IterationCount = 220_000 }));
    private readonly string _hashSustituto;

    public ProteccionContrasenas()
    {
        _hashSustituto = CrearHash(Guid.NewGuid().ToString("N"));
    }

    public string CrearHash(string contrasena) => _hasher.HashPassword(_usuario, contrasena);

    public bool Verificar(string? hash, string contrasena)
    {
        var resultado = _hasher.VerifyHashedPassword(_usuario, hash ?? _hashSustituto, contrasena);
        return hash is not null && resultado != PasswordVerificationResult.Failed;
    }
}
