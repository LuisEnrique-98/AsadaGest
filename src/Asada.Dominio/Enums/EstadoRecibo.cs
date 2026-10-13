namespace Asada.Dominio.Enums;

/// <summary>
/// Estado de un recibo. En el Sprint 3 todos los recibos quedan Emitidos; la anulacion (con
/// permiso de usuario autorizado y auditoria) llega en el Sprint 5. Un recibo nunca se borra.
/// </summary>
public enum EstadoRecibo
{
    Emitido = 1,
    Anulado = 2,
}
