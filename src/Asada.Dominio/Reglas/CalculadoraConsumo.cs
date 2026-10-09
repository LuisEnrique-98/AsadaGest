namespace Asada.Dominio.Reglas;

/// <summary>Ultima lectura registrada de un servicio (la base para calcular la siguiente).</summary>
public sealed record LecturaAnterior(decimal Lectura, int MedidorId, int Anio, int Mes, DateOnly FechaLectura);

public sealed record ResultadoConsumo(bool EsValido, decimal Consumo, string? Error)
{
    public static ResultadoConsumo Ok(decimal consumo) => new(true, consumo, null);
    public static ResultadoConsumo Falla(string error) => new(false, 0m, error);
}

/// <summary>
/// Calculo del consumo de un servicio entre su lectura anterior y la actual.
///
/// Caso normal (un solo medidor): consumo = lectura actual - lectura anterior.
///
/// Cambio de medidor entre dos lecturas (RB-013): una resta directa daria un valor negativo o
/// absurdo, porque el medidor nuevo empieza en otra escala. Se suma tramo por tramo:
///   (lectura final del medidor saliente - lectura anterior)
/// + (lectura final - lectura inicial de cada medidor intermedio, si hubo mas de un cambio)
/// + (lectura actual - lectura inicial del medidor entrante).
///
/// Sin lectura anterior (primera lectura del servicio) la base es la lectura inicial del medidor.
///
/// Es una funcion pura, sin base de datos: se prueba con xUnit sin infraestructura.
/// </summary>
public static class CalculadoraConsumo
{
    /// <param name="anterior">Ultima lectura del servicio, o null si es la primera.</param>
    /// <param name="cadenaMedidores">
    /// Medidores del servicio en orden de instalacion, desde el que tomo la lectura anterior
    /// (o el primero del servicio si no hay anterior) hasta el medidor actual, ambos inclusive.
    /// </param>
    /// <param name="lecturaActual">Lectura tomada ahora con el ultimo medidor de la cadena.</param>
    public static ResultadoConsumo Calcular(
        LecturaAnterior? anterior,
        IReadOnlyList<Entidades.Medidor> cadenaMedidores,
        decimal lecturaActual)
    {
        if (cadenaMedidores.Count == 0)
            return ResultadoConsumo.Falla("El servicio no tiene un medidor con el cual calcular el consumo.");

        if (anterior is not null && cadenaMedidores[0].Id != anterior.MedidorId)
            return ResultadoConsumo.Falla("El historial de medidores no coincide con la lectura anterior del servicio.");

        decimal total = 0m;

        for (var i = 0; i < cadenaMedidores.Count; i++)
        {
            var medidor = cadenaMedidores[i];
            var esPrimero = i == 0;
            var esUltimo = i == cadenaMedidores.Count - 1;

            var inicio = esPrimero && anterior is not null ? anterior.Lectura : medidor.LecturaInicial;

            decimal fin;
            if (esUltimo)
            {
                fin = lecturaActual;
            }
            else if (medidor.LecturaFinal is decimal final)
            {
                fin = final;
            }
            else
            {
                return ResultadoConsumo.Falla(
                    $"El medidor {medidor.NumeroSerie} fue reemplazado pero no tiene lectura final registrada.");
            }

            var tramo = fin - inicio;
            if (tramo < 0)
            {
                return ResultadoConsumo.Falla(cadenaMedidores.Count == 1
                    ? "La lectura no puede ser menor que la lectura anterior. Si se cambio el medidor, registre primero el retiro del medidor anterior y la instalacion del nuevo."
                    : $"El consumo del medidor {medidor.NumeroSerie} resulta negativo; revise las lecturas del cambio de medidor.");
            }

            total += tramo;
        }

        return ResultadoConsumo.Ok(total);
    }
}
