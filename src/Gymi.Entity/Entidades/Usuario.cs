using System.Globalization;

namespace Gymi.Entity;

public abstract class Usuario
{
    protected Usuario() { }

    protected Usuario(string nombre, string apellido, string email)
    {
        ActualizarDatos(nombre, apellido, email);
    }

    public int Id { get; private set; }
    public string Nombre { get; private set; } = "";
    public string Apellido { get; private set; } = "";
    public string Email { get; private set; } = "";
    public string? PasswordHash { get; private set; }
    public string? EmailAccesoNormalizado { get; private set; }
    public bool Activo { get; private set; } = true;
    public Guid Version { get; private set; }

    public bool AccesoWebHabilitado => Activo && EmailAccesoNormalizado is not null && PasswordHash is not null;

    public RolUsuario Rol => this switch
    {
        Administrador => RolUsuario.Administrador,
        Recepcionista => RolUsuario.Recepcionista,
        Cliente => RolUsuario.Cliente,
        _ => throw new InvalidOperationException("El tipo de usuario no tiene un rol configurado.")
    };

    public static string NormalizarEmail(string email)
    {
        var direccion = email.Trim();
        var separador = direccion.LastIndexOf('@');
        if (separador > 0 && NormalizarDominioEmail(direccion[(separador + 1)..]) is { } dominio)
        {
            return $"{direccion[..separador].ToUpperInvariant()}@{dominio}";
        }

        // Las cuentas guardadas antes de la validación estricta conservan su clave de acceso.
        return direccion.ToUpperInvariant();
    }

    public static string? NormalizarDominioEmail(string dominio)
    {
        string ascii;
        try
        {
            ascii = new IdnMapping { UseStd3AsciiRules = true }.GetAscii(dominio);
        }
        catch (ArgumentException)
        {
            return null;
        }

        var etiquetas = ascii.Split('.');
        return etiquetas.Length > 1 && etiquetas.All(etiqueta =>
            etiqueta.Length is >= 1 and <= 63 && etiqueta[0] != '-' && etiqueta[^1] != '-' &&
            etiqueta.All(caracter => char.IsAsciiLetterOrDigit(caracter) || caracter == '-'))
            ? ascii.ToUpperInvariant()
            : null;
    }

    public void HabilitarAcceso(string passwordHash)
    {
        if (!Activo || AccesoWebHabilitado)
        {
            throw new InvalidOperationException("El usuario no admite la activación.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(passwordHash.Length, 255);
        PasswordHash = passwordHash;
        EmailAccesoNormalizado = NormalizarEmail(Email);
        Version = Guid.NewGuid();
    }

    protected void ReservarEmailDeAcceso() => EmailAccesoNormalizado = NormalizarEmail(Email);

    protected void ActualizarDatos(string nombre, string apellido, string email)
    {
        if (!Activo)
        {
            throw new InvalidOperationException("El usuario está desactivado.");
        }

        Nombre = nombre;
        Apellido = apellido;
        Email = email;
        if (EmailAccesoNormalizado is not null)
        {
            EmailAccesoNormalizado = NormalizarEmail(email);
        }

        Version = Guid.NewGuid();
    }

    public void Desactivar()
    {
        if (Activo)
        {
            Activo = false;
            Version = Guid.NewGuid();
        }
    }
}

public enum RolUsuario
{
    Administrador,
    Recepcionista,
    Cliente
}
