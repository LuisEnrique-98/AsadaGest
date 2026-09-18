using Microsoft.AspNetCore.Identity;

namespace Asada.Infraestructura.Identidad;

/// <summary>
/// Rol de la plataforma (Superadministrador, Administrador, Operador, Consulta -- SRS &#167;23).
/// Extiende IdentityRole&lt;int&gt; por la misma razon que Usuario extiende IdentityUser&lt;int&gt;:
/// reutilizar la infraestructura de ASP.NET Core Identity ya probada.
/// </summary>
public class Rol : IdentityRole<int>
{
    public Rol() { }

    public Rol(string nombre) : base(nombre) { }
}
