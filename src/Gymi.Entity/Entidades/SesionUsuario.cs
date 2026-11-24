namespace Gymi.Entity;

public sealed class SesionUsuario
{
    private SesionUsuario() { }

    public SesionUsuario(Usuario usuario, string tokenHash, DateTimeOffset venceEn)
    {
        if (!usuario.AccesoWebHabilitado)
        {
            throw new InvalidOperationException("El usuario no tiene acceso web.");
        }

        UsuarioId = usuario.Id;
        VersionUsuario = usuario.Version;
        TokenHash = tokenHash;
        VenceEn = venceEn;
    }

    public string TokenHash { get; private set; } = "";
    public int UsuarioId { get; private set; }
    public Guid VersionUsuario { get; private set; }
    public DateTimeOffset VenceEn { get; private set; }
    public Usuario Usuario { get; private set; } = null!;
}
