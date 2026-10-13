namespace Asada.Dominio.Enums;

/// <summary>
/// Como se cobra la tarifa de hidrantes en el recibo. Se deja configurable por version de
/// tarifa (en vez de fijarlo en el codigo) porque el monto y la forma de cobro los define el
/// pliego tarifario vigente de cada ASADA.
/// </summary>
public enum ModoHidrante
{
    /// <summary>La tarifa no cobra hidrantes.</summary>
    NoAplica = 1,

    /// <summary>Un monto por cada m³ consumido.</summary>
    PorMetroCubico = 2,

    /// <summary>Un monto fijo por recibo, sin importar el consumo.</summary>
    MontoFijo = 3,
}
