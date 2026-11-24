using System.Net;
using System.Text.Json.Serialization;
using Gymi.Repositories;
using Gymi.API;
using Gymi.API.Controllers;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(opciones => opciones.Limits.MaxRequestBodySize = 16 * 1024);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.None);
builder.Logging.AddFilter("Microsoft.AspNetCore.Hosting.Diagnostics", LogLevel.None);
builder.Logging.AddFilter("Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware", LogLevel.None);
builder.Logging.AddFilter("Microsoft.AspNetCore.Server.Kestrel", LogLevel.Critical);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ManejadorExcepciones>();
builder.Services.ConfigureHttpJsonOptions(opciones =>
{
    opciones.SerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
    opciones.SerializerOptions.AllowDuplicateProperties = false;
    opciones.SerializerOptions.NumberHandling = JsonNumberHandling.Strict;
    opciones.SerializerOptions.Converters.Add(new ImporteJson());
});
builder.Services.Configure<RouteHandlerOptions>(opciones => opciones.ThrowOnBadRequest = true);
builder.Services.AgregarGymi(builder.Configuration.GetConnectionString("Gymi") ?? "Data Source=gymi.db");

if (builder.Environment.IsDevelopment() && args is not ["crear-administrador"])
{
    builder.Services.ConfigurarAcceso(new TokensJwt(builder.Configuration, builder.Environment));
    // La firma JWT usa su propia clave. El host de desarrollo no escribe un anillo de Data Protection.
    builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
    builder.Services.AddOpenApi(opciones => opciones
        .AddSchemaTransformer(ImporteJson.DescribirEsquema)
        .AddSchemaTransformer(PlanesOpenApi.DescribirEsquema)
        .AddDocumentTransformer(JwtOpenApi.AgregarEsquema)
        .AddDocumentTransformer((documento, _, _) =>
        {
            if (documento.Tags is { } etiquetas)
            {
                documento.Tags = new SortedSet<OpenApiTag>(etiquetas, Comparer<OpenApiTag>.Create((primera, segunda) =>
                {
                    var prioridad = PrioridadModulo(primera?.Name).CompareTo(PrioridadModulo(segunda?.Name));
                    return prioridad != 0 ? prioridad : string.CompareOrdinal(primera?.Name, segunda?.Name);
                }));
            }

            return Task.CompletedTask;
        })
        .AddOperationTransformer(JwtOpenApi.MarcarOperacionProtegida));
}

var app = builder.Build();
if (args is ["crear-administrador"])
{
    if (!app.Environment.IsDevelopment())
    {
        Console.Error.WriteLine("La provisión local requiere el entorno Development.");
        return 1;
    }

    return await InicializarBaseAsync(app.Services)
        ? await PrimerAdministrador.CrearAsync(app.Services)
        : 1;
}

app.UseExceptionHandler();
app.UseStatusCodePages(async contexto =>
{
    var http = contexto.HttpContext;
    await ErroresHttp.DesdeEstado(http, http.Response.StatusCode).ExecuteAsync(http);
});
app.UseRouting();

if (app.Environment.IsDevelopment())
{
    if (!await InicializarBaseAsync(app.Services))
    {
        return 1;
    }

    app.Use(async (contexto, siguiente) =>
    {
        if (contexto.Connection.RemoteIpAddress is not { } ip || !IPAddress.IsLoopback(ip))
        {
            contexto.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        contexto.Response.Headers.CacheControl = "no-store";
        await siguiente(contexto);
    });
    app.Use(SesionHttp.VerificarOrigenAsync);
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapOpenApi();
    app.UseSwaggerUI(opciones =>
    {
        opciones.DocumentTitle = "GYMI API";
        opciones.SwaggerEndpoint("../openapi/v1.json", "GYMI API v1");
    });
    app.MapGet("/", () => Results.Redirect("/swagger/index.html")).ExcludeFromDescription();
    app.MapearAcceso();
    app.MapearRecepcionistas();
    app.MapearClientes();
    app.MapearPlanes();
    app.MapearMembresias();
    app.MapearPagos();
    app.MapearAsistencias();
    app.MapearReportes();
}

await app.RunAsync();
return 0;

static int PrioridadModulo(string? nombre) => nombre switch
{
    "Clientes" => 0,
    "Planes" => 1,
    "Membresias" => 2,
    "Pagos" => 3,
    "Asistencias" => 4,
    "Alertas" => 5,
    "Reportes" => 6,
    "Acceso" => 7,
    "Recepcionistas" => 8,
    "Mi membresía" => 9,
    _ => int.MaxValue
};

static async Task<bool> InicializarBaseAsync(IServiceProvider servicios)
{
    if (await servicios.InicializarBaseLocalAsync())
    {
        return true;
    }

    Console.Error.WriteLine("La actualización de GYMI detectó cuentas con correos de acceso equivalentes. " +
        "Revisá las reservas en la base antes de volver a iniciar.");
    return false;
}
