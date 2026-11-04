using Asada.Dominio.Entidades;
using Asada.Dominio.Enums;
using Microsoft.AspNetCore.Identity;

namespace Asada.Infraestructura.Identidad;

/// <summary>
/// Usuario de la plataforma. Extiende IdentityUser&lt;int&gt; para apoyarse en la
/// implementacion de ASP.NET Core Identity (hash de contrasena, bloqueo por intentos
/// fallidos, tokens de confirmacion/recuperacion) en vez de reescribirla desde cero.
///
/// Esta clase vive en Asada.Infraestructura, no en Asada.Dominio, a proposito: es una
/// decision deliberada frente al planteamiento original del documento tecnico (que la
/// ubicaba en Dominio). La autenticacion es, en si misma, un detalle de infraestructura
/// (esta acoplada a ASP.NET Core Identity); mantener la regla de "Dominio sin ninguna
/// dependencia externa" es mas importante que la ubicacion original, y ningun caso de uso
/// de Asada.Aplicacion necesita conocer esta clase todavia (cuando lo necesite, se le
/// definira una interfaz propia en Aplicacion, como ya existe con IProveedorOrganizacion).
///
/// Nota: el int? OrganizacionId es null unicamente para el Superadministrador de la
/// plataforma, que no pertenece a ninguna ASADA.
/// </summary>
public class Usuario : IdentityUser<int>
{
    public int? OrganizacionId { get; set; }
    public Organizacion? Organizacion { get; set; }

    public required string Nombre { get; set; }

    public EstadoUsuario Estado { get; set; } = EstadoUsuario.Activo;

    public DateTime? UltimoAcceso { get; set; }

    /// <summary>
    /// Sprint 5b: true cuando la contrasena actual es temporal (la genero el sistema al crear el
    /// usuario o al restablecerla). Mientras sea true, el usuario solo puede entrar a la pagina de
    /// cambiar contrasena.
    /// </summary>
    public bool DebeCambiarContrasena { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
