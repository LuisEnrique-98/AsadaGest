namespace Asada.Aplicacion.Common;

/// <summary>
/// Resuelve a que organizacion (ASADA) pertenece el usuario autenticado de la solicitud actual.
/// Es la pieza central del aislamiento multiorganizacion (RB-015 del SRS): AsadaDbContext, en
/// Asada.Infraestructura, usa este valor en un HasQueryFilter global para que ninguna consulta
/// pueda devolver, por accidente, filas de otra organizacion.
///
/// La interfaz vive aqui, en Asada.Aplicacion, porque es un contrato que la capa de aplicacion
/// necesita (los casos de uso futuros la van a inyectar); la implementacion concreta, que lee
/// el usuario autenticado desde HttpContext, vive en Asada.Infraestructura.
/// </summary>
public interface IProveedorOrganizacion
{
    /// <summary>
    /// Id de la organizacion del usuario autenticado, o null si no hay un usuario autenticado
    /// en el contexto actual, o si el usuario autenticado es el Superadministrador (que no
    /// pertenece a ninguna organizacion y por lo tanto no debe quedar sujeto al filtro).
    /// </summary>
    int? OrganizacionId { get; }

    /// <summary>True cuando el usuario autenticado es el Superadministrador de la plataforma.</summary>
    bool EsSuperadministrador { get; }
}
