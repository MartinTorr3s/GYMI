namespace Gymi.Entity;

public sealed class ActivacionUsuario
{
    private ActivacionUsuario() { }

    public ActivacionUsuario(Usuario usuario, string tokenHash, DateTimeOffset venceEn)
    {
        if (usuario is not (Cliente or Recepcionista) || !usuario.Activo || usuario.AccesoWebHabilitado)
        {
            throw new InvalidOperationException("El usuario no admite la activación.");
        }

        UsuarioId = usuario.Id;
        VersionUsuario = usuario.Version;
        TokenHash = tokenHash;
        VenceEn = venceEn;
    }

    public int UsuarioId { get; private set; }
    public Guid VersionUsuario { get; private set; }
    public string TokenHash { get; private set; } = "";
    public DateTimeOffset VenceEn { get; private set; }
    public Usuario Usuario { get; private set; } = null!;
}
