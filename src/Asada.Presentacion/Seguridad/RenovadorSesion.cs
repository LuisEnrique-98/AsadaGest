using Asada.Infraestructura.Identidad;
using Microsoft.AspNetCore.Identity;

namespace Asada.Presentacion.Seguridad;

/// <summary>
/// Vuelve a emitir la cookie de sesion del usuario actual con sus datos de hoy. Hace falta despues de
/// cambiar la propia contrasena: el cambio renueva el sello de seguridad (lo que cierra cualquier otra
/// sesion abierta con la contrasena anterior) y quita la marca de contrasena temporal; sin renovar la
/// cookie, la propia sesion tambien se cerraria al minuto.
/// </summary>
public interface IRenovadorSesion
{
    Task RenovarAsync();
}

public sealed class RenovadorSesion(
    SignInManager<Usuario> signInManager, UserManager<Usuario> userManager, IHttpContextAccessor httpContextAccessor)
    : IRenovadorSesion
{
    public async Task RenovarAsync()
    {
        var principal = httpContextAccessor.HttpContext?.User;
        if (principal is null) return;
        var usuario = await userManager.GetUserAsync(principal);
        if (usuario is not null) await signInManager.RefreshSignInAsync(usuario);
    }
}
