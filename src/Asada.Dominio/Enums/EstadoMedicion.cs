namespace Asada.Dominio.Enums;

/// <summary>
/// Estado de una lectura. Una lectura nunca se borra: si se registro por error (otro servicio,
/// otro periodo) se anula con un motivo, y el periodo queda libre para registrarla de nuevo.
/// Las lecturas anuladas no cuentan para nada (consumo, facturacion, lectura anterior).
/// </summary>
public enum EstadoMedicion
{
    Vigente = 1,
    Anulada = 2,
}
