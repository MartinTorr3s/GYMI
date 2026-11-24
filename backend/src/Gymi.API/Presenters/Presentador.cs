using Gymi.UseCase;

namespace Gymi.API.Presenters;

public sealed class Presentador<T> : IPresentador<T>
{
    private Resultado<T>? resultado;

    public Resultado<T> Resultado =>
        resultado ?? throw new InvalidOperationException("La operación todavía no presentó un resultado.");

    public void Presentar(Resultado<T> resultado) => this.resultado = resultado;
}
