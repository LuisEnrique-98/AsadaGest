namespace Asada.Dominio.Enums;

/// <summary>
/// Estado de un recibo. Se emite al generarse; el Administrador lo puede anular con un motivo
/// (Sprint 5). Un recibo nunca se borra.
/// </summary>
public enum EstadoRecibo
{
    Emitido = 1,
    Anulado = 2,
}
