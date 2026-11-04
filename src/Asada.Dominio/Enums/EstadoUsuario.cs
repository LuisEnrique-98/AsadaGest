namespace Asada.Dominio.Enums;

/// <summary>
/// Estado de un usuario del sistema. Un usuario inactivo no puede iniciar sesion, pero no se borra:
/// su nombre sigue en la auditoria y en las lecturas y recibos que registro (Sprint 5b).
/// </summary>
public enum EstadoUsuario
{
    Activo = 1,
    Inactivo = 2,
}
