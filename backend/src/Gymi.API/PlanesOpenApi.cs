using System.Globalization;
using System.Text.Json.Nodes;
using Gymi.UseCase;
using Gymi.Entity;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Gymi.API;

internal static class PlanesOpenApi
{
    public static Task DescribirEsquema(OpenApiSchema esquema, OpenApiSchemaTransformerContext contexto,
        CancellationToken ct)
    {
        if (contexto.JsonTypeInfo.Type == typeof(DatosPlan))
        {
            esquema.Required = new HashSet<string>(["nombre", "descripcion", "precio", "activo"]);
            esquema.Properties!["nombre"] = new OpenApiSchema { Type = JsonSchemaType.String };
            esquema.Properties["descripcion"] = new OpenApiSchema { Type = JsonSchemaType.String };
            esquema.Properties["precio"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Number,
                Minimum = "0",
                Maximum = PlanMembresia.PrecioMaximo.ToString(CultureInfo.InvariantCulture),
                Description = "Importe con hasta dos decimales."
            };
            esquema.Properties["activo"] = new OpenApiSchema { Type = JsonSchemaType.Boolean };
            esquema.OneOf =
            [
                new OpenApiSchema
                {
                    Title = "Por mes calendario",
                    Required = new HashSet<string>(["tipoDuracion", "cantidadSesiones"]),
                    Properties = new Dictionary<string, IOpenApiSchema>
                    {
                        ["tipoDuracion"] = TipoDuracion(TiposDuracionPlan.MesCalendario),
                        ["cantidadSesiones"] = EnteroPositivo(int.MaxValue),
                        ["duracionDias"] = new OpenApiSchema { Type = JsonSchemaType.Null },
                        ["sesionesPorSemana"] = new OpenApiSchema { Type = JsonSchemaType.Null }
                    }
                },
                new OpenApiSchema
                {
                    Title = "Por días",
                    Required = new HashSet<string>(["duracionDias", "cantidadSesiones"]),
                    Properties = new Dictionary<string, IOpenApiSchema>
                    {
                        ["tipoDuracion"] = new OpenApiSchema
                        {
                            AnyOf = [TipoDuracion(TiposDuracionPlan.Dias), new OpenApiSchema { Type = JsonSchemaType.Null }]
                        },
                        ["duracionDias"] = EnteroPositivo(PlanMembresia.DuracionMaximaDias),
                        ["cantidadSesiones"] = EnteroPositivo(int.MaxValue),
                        ["sesionesPorSemana"] = new OpenApiSchema { Type = JsonSchemaType.Null }
                    }
                },
                new OpenApiSchema
                {
                    Title = "Por mes calendario con el formato anterior",
                    Description = "Compatibilidad con solicitudes que enviaban solo la frecuencia semanal.",
                    Required = new HashSet<string>(["tipoDuracion", "sesionesPorSemana"]),
                    Properties = new Dictionary<string, IOpenApiSchema>
                    {
                        ["tipoDuracion"] = TipoDuracion(TiposDuracionPlan.MesCalendario),
                        ["sesionesPorSemana"] = EnteroPositivo(PlanMembresia.MaximoSesionesPorSemana),
                        ["duracionDias"] = new OpenApiSchema { Type = JsonSchemaType.Null },
                        ["cantidadSesiones"] = new OpenApiSchema { Type = JsonSchemaType.Null }
                    }
                }
            ];
        }
        else if (contexto.JsonTypeInfo.Type == typeof(DatosPrevisualizacionPlanMensual))
        {
            esquema.Required = new HashSet<string>(["sesionesPorSemana"]);
            esquema.Properties!["sesionesPorSemana"] = EnteroPositivo(PlanMembresia.MaximoSesionesPorSemana);
        }

        return Task.CompletedTask;
    }

    public static Task EjemplosDeAltaYEdicion(OpenApiOperation operacion,
        OpenApiOperationTransformerContext contexto, CancellationToken ct)
    {
        if (operacion.RequestBody?.Content?.TryGetValue("application/json", out var cuerpo) == true)
        {
            cuerpo.Examples = new Dictionary<string, IOpenApiExample>
            {
                ["mesCalendario"] = new OpenApiExample
                {
                    Summary = "Por mes calendario",
                    Value = new JsonObject
                    {
                        ["nombre"] = "Plan mensual",
                        ["descripcion"] = "Diez sesiones para usar durante la vigencia",
                        ["precio"] = 24900.50m,
                        ["activo"] = true,
                        ["tipoDuracion"] = TiposDuracionPlan.MesCalendario,
                        ["cantidadSesiones"] = 10
                    }
                },
                ["dias"] = new OpenApiExample
                {
                    Summary = "Por días",
                    Value = new JsonObject
                    {
                        ["nombre"] = "Pase de 15 días",
                        ["descripcion"] = "Cinco sesiones durante la vigencia",
                        ["precio"] = 14900m,
                        ["activo"] = true,
                        ["tipoDuracion"] = TiposDuracionPlan.Dias,
                        ["duracionDias"] = 15,
                        ["cantidadSesiones"] = 5
                    }
                }
            };
        }

        return Task.CompletedTask;
    }

    public static Task EjemploDePrevisualizacion(OpenApiOperation operacion,
        OpenApiOperationTransformerContext contexto, CancellationToken ct)
    {
        if (operacion.RequestBody?.Content?.TryGetValue("application/json", out var cuerpo) == true)
        {
            cuerpo.Example = new JsonObject { ["sesionesPorSemana"] = 3 };
        }

        return Task.CompletedTask;
    }

    private static OpenApiSchema TipoDuracion(string tipo) =>
        new() { Type = JsonSchemaType.String, Const = tipo };

    private static OpenApiSchema EnteroPositivo(int maximo) => new()
    {
        Type = JsonSchemaType.Integer,
        Format = "int32",
        Minimum = "1",
        Maximum = maximo.ToString(CultureInfo.InvariantCulture)
    };
}
