namespace Asada.Dominio.Reglas;

/// <summary>
/// Deteccion de consumo anormal (SRS seccion 25, correccion C-05): un consumo es anormal si
/// supera el promedio de los ultimos 3 consumos en mas de un porcentaje. Es solo informativo:
/// marca la lectura para revision, no bloquea la facturacion.
/// El porcentaje (50 % por defecto) lo configura cada ASADA (Organizacion.PorcentajeConsumoAnormal).
/// </summary>
public static class ReglasConsumoAnormal
{
    public const int PeriodosReferencia = 3;
    public const decimal PorcentajeUmbralPorDefecto = 50m;
    public const decimal PorcentajeUmbralMinimo = 10m;
    public const decimal PorcentajeUmbralMaximo = 1000m;

    public static bool EsUmbralValido(decimal porcentaje)
        => porcentaje >= PorcentajeUmbralMinimo && porcentaje <= PorcentajeUmbralMaximo;

    /// <param name="consumosAnteriores">Los consumos previos del servicio, del mas reciente al mas antiguo.</param>
    public static bool EsAnormal(
        decimal consumo, IReadOnlyList<decimal> consumosAnteriores,
        decimal porcentajeUmbral = PorcentajeUmbralPorDefecto)
    {
        // Con menos de 3 consumos previos no hay historia suficiente para opinar.
        if (consumosAnteriores.Count < PeriodosReferencia) return false;

        var promedio = consumosAnteriores.Take(PeriodosReferencia).Average();
        if (promedio <= 0m) return false;

        return consumo > promedio * (1m + porcentajeUmbral / 100m);
    }
}
