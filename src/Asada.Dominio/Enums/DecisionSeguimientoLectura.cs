namespace Asada.Dominio.Enums;

/// <summary>
/// Decision del operador sobre un servicio al que no se le tomo lectura en el periodo.
/// </summary>
public enum DecisionSeguimientoLectura
{
    /// <summary>Tomar la lectura lo antes posible, aunque ya haya pasado la ventana.</summary>
    LeerLoAntesPosible = 1,

    /// <summary>No tomarla ahora; se espera al periodo del mes siguiente.</summary>
    EsperarAlSiguienteMes = 2,
}
