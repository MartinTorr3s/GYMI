using System.Security.Cryptography;
using System.Text;

namespace Gymi.API;

internal static class ClaveJwt
{
    public static byte[] Cargar(IConfiguration configuracion, IWebHostEnvironment entorno)
    {
        if (configuracion["Jwt:ClaveBase64"] is { } configurada)
        {
            if (string.IsNullOrWhiteSpace(configurada))
            {
                throw new InvalidOperationException("Configurá Jwt:ClaveBase64 con una clave válida.");
            }

            byte[] clave;
            try
            {
                clave = Convert.FromBase64String(configurada);
            }
            catch (FormatException)
            {
                throw new InvalidOperationException("Jwt:ClaveBase64 debe estar en Base64.");
            }

            if (clave.Length < 32)
            {
                throw new InvalidOperationException("Jwt:ClaveBase64 debe contener al menos 32 bytes.");
            }

            return clave;
        }

        if (!entorno.IsDevelopment())
        {
            throw new InvalidOperationException("Configurá Jwt:ClaveBase64 para iniciar la autenticación.");
        }

        var archivo = Path.Combine(entorno.ContentRootPath, ".gymi-jwt.key");
        if (!File.Exists(archivo))
        {
            PublicarClaveLocal(archivo);
        }

        var local = File.ReadAllBytes(archivo);
        if (local.Length != 32)
        {
            throw new InvalidOperationException("El archivo de firma JWT local debe contener 32 bytes.");
        }

        return local;
    }

    private static void PublicarClaveLocal(string archivo)
    {
        var temporal = $"{archivo}.{Guid.NewGuid():N}.tmp";
        try
        {
            var opciones = new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write };
            if (!OperatingSystem.IsWindows())
            {
                opciones.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
            }

            using (var flujo = new FileStream(temporal, opciones))
            {
                flujo.Write(RandomNumberGenerator.GetBytes(32));
            }

            // El nombre definitivo solo existe cuando la clave completa está escrita.
            File.Move(temporal, archivo, overwrite: false);
        }
        catch (IOException) when (File.Exists(archivo))
        {
            // Otro proceso inicializó la clave compartida.
        }
        finally
        {
            File.Delete(temporal);
        }
    }
}
