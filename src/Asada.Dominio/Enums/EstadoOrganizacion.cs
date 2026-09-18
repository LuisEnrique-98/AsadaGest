namespace Asada.Dominio.Enums;

/// <summary>
/// Estado operativo de una organizacion (ASADA) dentro de la plataforma.
/// Una organizacion Inactiva no debe permitir el inicio de sesion de sus usuarios,
/// pero sus datos historicos se conservan (no hay borrado fisico, ver SRS &#167;36).
/// </summary>
public enum EstadoOrganizacion
{
    Activa = 1,
    Inactiva = 2,
}
