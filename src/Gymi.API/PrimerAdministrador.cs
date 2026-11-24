using Gymi.UseCase;

namespace Gymi.API;

internal static class PrimerAdministrador
{
    public static async Task<int> CrearAsync(IServiceProvider servicios)
    {
        await using var scope = servicios.CreateAsyncScope();
        var provision = scope.ServiceProvider.GetRequiredService<IProvisionAdministrador>();
        var nombre = Leer("Nombre");
        var apellido = Leer("Apellido");
        var email = Leer("Correo");
        var datos = new DatosPrimerAdministrador(nombre, apellido, email, LeerContrasena());
        var resultado = await provision.CrearPrimeroAsync(datos, CancellationToken.None);
        if (resultado.Error is not { } error)
        {
            Console.WriteLine("Se creó el primer administrador.");
            return 0;
        }

        Console.Error.WriteLine(error.Mensaje);
        if (error.Campos is not null)
        {
            foreach (var campo in error.Campos)
            {
                Console.Error.WriteLine($"{campo.Key} — {string.Join(" ", campo.Value)}");
            }
        }

        return 1;
    }

    private static string? Leer(string etiqueta)
    {
        Console.Write($"{etiqueta}: ");
        return Console.ReadLine();
    }

    private static string? LeerContrasena()
    {
        Console.Write("Contraseña: ");
        if (Console.IsInputRedirected)
        {
            return Console.ReadLine();
        }

        var caracteres = new List<char>();
        while (true)
        {
            var tecla = Console.ReadKey(intercept: true);
            if (tecla.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                return new string(caracteres.ToArray());
            }

            if (tecla.Key == ConsoleKey.Backspace && caracteres.Count > 0)
            {
                caracteres.RemoveAt(caracteres.Count - 1);
            }
            else if (!char.IsControl(tecla.KeyChar))
            {
                caracteres.Add(tecla.KeyChar);
            }
        }
    }
}
