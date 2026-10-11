namespace Asada.Dominio.Enums;

/// <summary>
/// Estado generico de un registro que no se elimina fisicamente (SRS: sin borrado fisico).
/// Lo usan Abonado, Propiedad y Servicio.
/// </summary>
public enum EstadoRegistro
{
    Activo = 1,
    Inactivo = 2,
}
