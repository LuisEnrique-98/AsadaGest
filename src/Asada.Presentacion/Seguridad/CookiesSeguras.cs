namespace Asada.Presentacion.Seguridad;

/// <summary>
/// Sprint 6c (M-2): las cookies de la sesion y del token antiforgery solo viajan por HTTPS (Secure) y,
/// fuera de Desarrollo, llevan el prefijo "__Host-": el navegador solo las acepta si vienen por HTTPS,
/// para todo el sitio y sin dominio, asi otro sitio (o un subdominio) no las puede reemplazar.
///
/// Al cambiar el nombre de las cookies, todos vuelven a iniciar sesion una vez despues de actualizar.
///
/// En Desarrollo se deja "segun la peticion" (Secure solo si se entra por https) y sin prefijo, para que
/// el perfil http de Visual Studio siga funcionando.
/// </summary>
public static class CookiesSeguras
{
    public const string NombreSesion = "AsadaGest.Sesion";
    public const string NombreAntiforgery = "AsadaGest.Antiforgery";
    public const string PrefijoHost = "__Host-";

    public static IServiceCollection AgregarCookiesSeguras(this IServiceCollection servicios, IHostEnvironment entorno)
    {
        var desarrollo = entorno.IsDevelopment();
        var seguridad = desarrollo ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        var prefijo = desarrollo ? "" : PrefijoHost;

        servicios.ConfigureApplicationCookie(opciones =>
        {
            opciones.Cookie.Name = prefijo + NombreSesion;
            opciones.Cookie.SecurePolicy = seguridad;
            opciones.Cookie.HttpOnly = true;
            opciones.Cookie.SameSite = SameSiteMode.Lax;
            opciones.Cookie.Path = "/";
        });

        servicios.AddAntiforgery(opciones =>
        {
            opciones.Cookie.Name = prefijo + NombreAntiforgery;
            opciones.Cookie.SecurePolicy = seguridad;
            opciones.Cookie.Path = "/";
        });

        return servicios;
    }
}
