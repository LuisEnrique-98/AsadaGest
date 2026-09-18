namespace Asada.Infraestructura.Identidad;

/// <summary>
/// Nombres de los claims propios de la plataforma que se agregan al ClaimsPrincipal
/// del usuario autenticado (ver UsuarioClaimsPrincipalFactory), y que luego lee
/// ProveedorOrganizacion para resolver el aislamiento multiorganizacion.
/// </summary>
public static class ClaimsAsada
{
    public const string OrganizacionId = "asada:organizacion_id";
}
