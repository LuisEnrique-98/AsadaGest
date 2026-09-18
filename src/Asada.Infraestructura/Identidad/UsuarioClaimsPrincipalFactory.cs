using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Asada.Infraestructura.Identidad;

/// <summary>
/// Al iniciar sesion, ASP.NET Core Identity arma el ClaimsPrincipal del usuario a traves
/// de esta fabrica. Se agrega aqui el claim personalizado con el OrganizacionId (se omite
/// si el usuario es el Superadministrador, que no pertenece a ninguna organizacion), para
/// que el resto de la aplicacion -- en particular ProveedorOrganizacion -- pueda leerlo sin
/// volver a consultar la base de datos en cada solicitud.
/// </summary>
public class UsuarioClaimsPrincipalFactory(
    UserManager<Usuario> userManager,
    RoleManager<Rol> roleManager,
    IOptions<IdentityOptions> optionsAccessor)
    : UserClaimsPrincipalFactory<Usuario, Rol>(userManager, roleManager, optionsAccessor)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(Usuario usuario)
    {
        var identidad = await base.GenerateClaimsAsync(usuario);

        identidad.AddClaim(new Claim(ClaimTypes.GivenName, usuario.Nombre));

        if (usuario.OrganizacionId is int organizacionId)
        {
            identidad.AddClaim(new Claim(ClaimsAsada.OrganizacionId, organizacionId.ToString()));
        }

        return identidad;
    }
}
