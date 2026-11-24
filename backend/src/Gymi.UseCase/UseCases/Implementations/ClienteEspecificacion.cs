using System.Net.Mail;
using Gymi.UseCase;
using Gymi.Entity;

namespace Gymi.UseCase.UseCases.Implementations;

internal sealed record DatosClienteValidados(string Nombre, string Apellido, string Email, string Dni, string Telefono);

internal static class ClienteEspecificacion
{
    public static Resultado<DatosClienteValidados> Validar(DatosCliente datos)
    {
        var errores = new Dictionary<string, string[]>();
        var nombre = ValidacionEntrada.Texto(datos.Nombre, "nombre", "El nombre", LimitesCliente.Nombre, errores);
        var apellido = ValidacionEntrada.Texto(datos.Apellido, "apellido", "El apellido", LimitesCliente.Apellido, errores);
        var email = ValidarEmail(datos.Email, errores);
        var dni = ValidarDni(datos.Dni, errores);
        var telefono = datos.Telefono?.Trim() ?? "";

        var digitos = telefono.StartsWith('+') ? telefono[1..] : telefono;
        if (telefono.Length == 0)
        {
            errores["telefono"] = ["El teléfono es obligatorio."];
        }
        else if (digitos.Length > LimitesCliente.TelefonoDigitos)
        {
            errores["telefono"] = [$"El teléfono debe tener como máximo {LimitesCliente.TelefonoDigitos} dígitos."];
        }
        else if (digitos.Length == 0 || !TieneSoloDigitos(digitos))
        {
            errores["telefono"] = ["Ingresá solo dígitos y, si corresponde, un signo + al inicio."];
        }

        return errores.Count == 0
            ? Resultado<DatosClienteValidados>.Exito(new(nombre, apellido, email, dni, telefono))
            : Resultado<DatosClienteValidados>.Fallo(ValidacionEntrada.Error(errores));
    }

    public static string ValidarEmail(string? entrada, Dictionary<string, string[]> errores,
        bool exigirDominioValido = true)
    {
        var email = ValidacionEntrada.Texto(entrada, "email", "El correo electrónico", LimitesCliente.Email, errores);
        if (!errores.ContainsKey("email") &&
            (!MailAddress.TryCreate(email, out var direccion) ||
             !string.Equals(email, direccion.Address, StringComparison.Ordinal) ||
             email.Any(char.IsWhiteSpace) || !direccion.Host.Contains('.') ||
             (exigirDominioValido && Usuario.NormalizarDominioEmail(direccion.Host) is null)))
        {
            errores["email"] = ["Ingresá un correo electrónico válido."];
        }

        return email;
    }

    public static Resultado<string?> ValidarBusqueda(string? entrada)
    {
        if (string.IsNullOrWhiteSpace(entrada))
        {
            return Resultado<string?>.Exito(null);
        }

        var errores = new Dictionary<string, string[]>();
        var consulta = ValidacionEntrada.Texto(entrada, "q", "La búsqueda",
            LimitesCliente.Nombre + LimitesCliente.Apellido + 1, errores);
        return errores.Count == 0
            ? Resultado<string?>.Exito(consulta)
            : Resultado<string?>.Fallo(ValidacionEntrada.Error(errores));
    }

    public static string ValidarDni(string? entrada, Dictionary<string, string[]> errores)
    {
        var dni = ValidacionEntrada.Texto(entrada, "dni", "El DNI", LimitesCliente.Dni, errores);
        if (!errores.ContainsKey("dni") && !TieneSoloDigitos(dni))
        {
            errores["dni"] = ["El DNI debe contener solo dígitos."];
        }

        return dni;
    }

    private static bool TieneSoloDigitos(string valor) => valor.All(caracter => caracter is >= '0' and <= '9');
}
