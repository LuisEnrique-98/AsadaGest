using Asada.Aplicacion.Common;
using Microsoft.AspNetCore.Http;

namespace Asada.Infraestructura.Identidad;

/// <summary>
/// Implementacion concreta de IProveedorOrganizacion: lee el claim "asada:organizacion_id"
/// (agregado en UsuarioClaimsPrincipalFactory al iniciar sesion) del usuario autenticado
/// de la solicitud HTTP actual.
///
/// Se registra con ciclo de vida "Scoped" (una instancia por solicitud), igual que
/// AsadaDbContext, para que el filtro de organizacion sea consistente durante toda
/// la solicitud.
///
/// Sprint 6c (M-1): para ser Superadministrador no basta con no tener el claim de organizacion;
/// la sesion tambien debe traer el rol Superadministrador. Asi, una sesion sin ese claim por un
/// error (un usuario mal creado, un cambio futuro en la fabrica de claims) no ve todas las ASADAs:
/// el filtro de la base no le devuelve nada.
/// </summary>
public class ProveedorOrganizacion(IHttpContextAccessor httpContextAccessor) : IProveedorOrganizacion
{
    public int? OrganizacionId
    {
        get
        {
            var usuario = httpContextAccessor.HttpContext?.User;
            var valorClaim = usuario?.FindFirst(ClaimsAsada.OrganizacionId)?.Value;

            return int.TryParse(valorClaim, out var organizacionId) ? organizacionId : null;
        }
    }

    public bool EsSuperadministrador
    {
        get
        {
            var usuario = httpContextAccessor.HttpContext?.User;
            return (usuario?.Identity?.IsAuthenticated ?? false)
                && OrganizacionId is null
                && usuario.IsInRole(NombresRol.Superadministrador);
        }
    }
}
