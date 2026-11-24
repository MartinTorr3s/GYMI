using Gymi.UseCase;
using Gymi.Entity;

namespace Gymi.UseCase.UseCases.Implementations;

internal sealed record DatosPlanValidados(string Nombre, string Descripcion, decimal Precio,
    int? DuracionDias, int CantidadSesiones, bool Activo);

internal static class PlanEspecificacion
{
    public static Resultado<DatosPlanValidados> Validar(DatosPlan datos)
    {
        var errores = new Dictionary<string, string[]>();
        var nombre = ValidacionEntrada.Texto(datos.Nombre, "nombre", "El nombre", PlanMembresia.NombreMaximo, errores);
        var descripcion = ValidacionEntrada.Texto(datos.Descripcion, "descripcion", "La descripción",
            PlanMembresia.DescripcionMaxima, errores);

        if (datos.Precio is not { } precio)
        {
            errores["precio"] = ["Ingresá el precio del plan."];
        }
        else if (!PlanMembresia.EsPrecioValido(precio))
        {
            errores["precio"] = ["El precio debe estar entre 0 y 99999999.99 y admitir hasta dos decimales."];
        }

        var cupo = datos.TipoDuracion switch
        {
            null or TiposDuracionPlan.Dias => ValidarPorDias(datos, errores),
            TiposDuracionPlan.MesCalendario => ValidarMensual(datos, errores),
            _ => RegistrarTipoInvalido(errores)
        };

        if (datos.Activo is null)
        {
            errores["activo"] = ["Indicá si el plan está activo."];
        }

        return errores.Count > 0
            ? Resultado<DatosPlanValidados>.Fallo(ValidacionEntrada.Error(errores))
            : Resultado<DatosPlanValidados>.Exito(new(nombre, descripcion, datos.Precio!.Value,
                datos.DuracionDias, cupo!.Value, datos.Activo!.Value));
    }

    public static Resultado<VistaPreviaPlanMensual> Previsualizar(int? sesionesPorSemana)
    {
        var errores = new Dictionary<string, string[]>();
        var cupo = ValidarFrecuenciaMensual(sesionesPorSemana, errores);
        return cupo is { } total
            ? Resultado<VistaPreviaPlanMensual>.Exito(
                new(TiposDuracionPlan.MesCalendario, sesionesPorSemana!.Value, total))
            : Resultado<VistaPreviaPlanMensual>.Fallo(ValidacionEntrada.Error(errores));
    }

    private static int? ValidarPorDias(DatosPlan datos, Dictionary<string, string[]> errores)
    {
        if (datos.DuracionDias is not { } dias || !PlanMembresia.EsDuracionValida(dias))
        {
            errores["duracionDias"] = [$"La duración en días debe estar entre 1 y {PlanMembresia.DuracionMaximaDias}."];
        }

        if (datos.SesionesPorSemana is not null)
        {
            errores["sesionesPorSemana"] = ["Para un plan por días, ingresá la cantidad de sesiones incluidas."];
        }

        return ValidarCupo(datos.CantidadSesiones, errores);
    }

    private static int? ValidarMensual(DatosPlan datos, Dictionary<string, string[]> errores)
    {
        if (datos.DuracionDias is not null)
        {
            errores["duracionDias"] = ["En un plan mensual, la duración se determina al asignar la membresía."];
        }

        if (datos.SesionesPorSemana is { } frecuencia)
        {
            if (datos.CantidadSesiones is not null)
            {
                errores["sesionesPorSemana"] = ["Enviá solo la cantidad de sesiones al confirmar el cupo."];
                return null;
            }

            // Compatibilidad con solicitudes mensuales anteriores al cupo editable.
            return ValidarFrecuenciaMensual(frecuencia, errores);
        }

        return ValidarCupo(datos.CantidadSesiones, errores);
    }

    private static int? ValidarCupo(int? cantidadSesiones, Dictionary<string, string[]> errores)
    {
        if (cantidadSesiones is null or <= 0)
        {
            errores["cantidadSesiones"] = ["La cantidad de sesiones incluidas debe ser un entero positivo."];
        }

        return cantidadSesiones;
    }

    private static int? ValidarFrecuenciaMensual(int? frecuencia, Dictionary<string, string[]> errores)
    {
        if (frecuencia is not (>= 1 and <= PlanMembresia.MaximoSesionesPorSemana))
        {
            errores["sesionesPorSemana"] =
                [$"Ingresá un entero entre 1 y {PlanMembresia.MaximoSesionesPorSemana}."];
            return null;
        }

        return PlanMembresia.CalcularCupoMensual(frecuencia.Value);
    }

    private static int? RegistrarTipoInvalido(Dictionary<string, string[]> errores)
    {
        errores["tipoDuracion"] = ["Elegí dias o mesCalendario."];
        return null;
    }
}
