using System.Globalization;
using System.Text;
using Gymi.UseCase;

namespace Gymi.UseCase.UseCases.Implementations;

internal static class ValidacionEntrada
{
    public static readonly ErrorAplicacion VersionDesactualizada =
        new(TipoError.Precondicion, "version_desactualizada",
            "La ficha cambió. Volvé a consultar sus datos antes de guardar.");

    public static ErrorAplicacion Error(Dictionary<string, string[]> errores) =>
        new(TipoError.Validacion, "validacion_fallida", "Revisá los datos enviados.", errores);

    public static ErrorAplicacion? ValidarId(int id) => id > 0
        ? null
        : Error(new() { ["id"] = ["El ID debe ser un entero positivo."] });

    public static Resultado<int> CalcularOffset(int pagina, int tamanoPagina)
    {
        var errores = new Dictionary<string, string[]>();
        if (pagina < 1)
        {
            errores["pagina"] = ["La página debe ser mayor o igual a 1."];
        }

        if (tamanoPagina is < 1 or > 100)
        {
            errores["tamanoPagina"] = ["El tamaño de página debe estar entre 1 y 100."];
        }

        var offset = (pagina - 1L) * tamanoPagina;
        if (errores.Count == 0 && offset > int.MaxValue)
        {
            errores["pagina"] = ["La página solicitada está fuera de rango."];
        }

        return errores.Count > 0 ? Resultado<int>.Fallo(Error(errores)) : Resultado<int>.Exito((int)offset);
    }

    public static string Texto(string? entrada, string campo, string etiqueta, int limite,
        Dictionary<string, string[]> errores)
    {
        var valor = entrada?.Trim() ?? "";
        if (valor.Length == 0)
        {
            errores[campo] = ["Completá este campo."];
        }
        else if (valor.Length > limite)
        {
            errores[campo] = [$"{etiqueta} debe tener como máximo {limite} caracteres."];
        }
        else
        {
            var error = ValidarContenido(valor, etiqueta);
            if (error is not null)
            {
                errores[campo] = [error];
            }
        }

        return valor;
    }

    private static string? ValidarContenido(string valor, string etiqueta)
    {
        var contieneTextoVisible = false;
        foreach (var caracter in valor.EnumerateRunes())
        {
            var categoria = Rune.GetUnicodeCategory(caracter);
            if (categoria == UnicodeCategory.Control ||
                (categoria == UnicodeCategory.Format && caracter.Value is not (0x200C or 0x200D)))
            {
                return $"{etiqueta} contiene caracteres inválidos.";
            }

            // Los rellenos braille y hangul tienen categoría de símbolo o letra, pero se ven vacíos.
            if (caracter.Value is not (0x2800 or 0x115F or 0x1160 or 0x3164 or 0xFFA0) &&
                (Rune.IsLetter(caracter) || Rune.IsNumber(caracter) ||
                 Rune.IsPunctuation(caracter) || Rune.IsSymbol(caracter)))
            {
                contieneTextoVisible = true;
            }
        }

        return contieneTextoVisible ? null : "Ingresá al menos un carácter visible.";
    }
}
