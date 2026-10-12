namespace Asada.Aplicacion.Common;

/// <summary>
/// Resultado de un caso de uso. Se usa en vez de excepciones para los errores de negocio
/// esperables (identificacion duplicada, dato invalido, registro no encontrado): la interfaz
/// de usuario los muestra tal cual, sin try/catch. Las excepciones quedan para fallos reales.
/// </summary>
public class Resultado
{
    protected Resultado(bool exitoso, IReadOnlyList<string> errores)
    {
        EsExitoso = exitoso;
        Errores = errores;
    }

    public bool EsExitoso { get; }
    public IReadOnlyList<string> Errores { get; }

    public static Resultado Ok() => new(true, []);
    public static Resultado Falla(params string[] errores) => new(false, errores);
    public static Resultado Falla(IEnumerable<string> errores) => new(false, errores.ToList());
}

public sealed class Resultado<T> : Resultado
{
    private Resultado(bool exitoso, T? valor, IReadOnlyList<string> errores) : base(exitoso, errores)
        => Valor = valor;

    public T? Valor { get; }

    public static Resultado<T> Ok(T valor) => new(true, valor, []);
    public new static Resultado<T> Falla(params string[] errores) => new(false, default, errores);
    public new static Resultado<T> Falla(IEnumerable<string> errores) => new(false, default, errores.ToList());
}
