using System.Buffers;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Gymi.API;

internal sealed class ImporteJson : JsonConverter<decimal>
{
    public static Task DescribirEsquema(OpenApiSchema esquema, OpenApiSchemaTransformerContext contexto,
        CancellationToken ct)
    {
        if (contexto.JsonTypeInfo.Type == typeof(decimal))
        {
            esquema.Type = JsonSchemaType.Number | (esquema.Type.GetValueOrDefault() & JsonSchemaType.Null);
        }
        else if (contexto.JsonTypeInfo.Type == typeof(decimal?))
        {
            esquema.Type = JsonSchemaType.Number | JsonSchemaType.Null;
        }

        return Task.CompletedTask;
    }

    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.Number || !reader.TryGetDecimal(out var importe))
        {
            throw new JsonException("Se esperaba un importe decimal.");
        }

        // El caso de uso conserva los errores por campo. Acá se detecta la precisión perdida al deserializar.
        if (decimal.Round(importe, 2) == importe &&
            !TieneCentavosExactos(reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan))
        {
            throw new JsonException("El importe no puede representarse exactamente en centavos.");
        }

        return importe;
    }

    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options) =>
        writer.WriteNumberValue(value);

    private static bool TieneCentavosExactos(ReadOnlySpan<byte> valor)
    {
        var indiceExponente = valor.IndexOfAny((byte)'e', (byte)'E');
        var significando = indiceExponente < 0 ? valor : valor[..indiceExponente];
        var indicePunto = significando.IndexOf((byte)'.');
        var cifrasFraccionarias = indicePunto < 0 ? 0 : significando.Length - indicePunto - 1;
        var cerosFinales = 0;
        var indice = significando.Length - 1;

        for (; indice >= 0; indice--)
        {
            var cifra = significando[indice];
            if (cifra == (byte)'0')
            {
                cerosFinales++;
            }
            else if (cifra != (byte)'.' && cifra != (byte)'-')
            {
                break;
            }
        }

        if (indice < 0)
        {
            return true; // Cualquier exponente de un significando cero sigue siendo cero.
        }

        var exponente = 0;
        return (indiceExponente < 0 || int.TryParse(valor[(indiceExponente + 1)..], NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture, out exponente))
            && (long)cifrasFraccionarias - exponente - cerosFinales <= 2;
    }
}
