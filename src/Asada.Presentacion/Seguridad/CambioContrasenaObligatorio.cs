using Asada.Infraestructura.Identidad;

namespace Asada.Presentacion.Seguridad;

/// <summary>
/// Sprint 5b: un usuario con contrasena temporal (recien creado, o con la contrasena restablecida por
/// el Administrador) solo puede entrar a la pagina de cambiar contrasena y cerrar sesion. Cualquier
/// otra pagina, enlace o descarga lo lleva ahi. La marca viaja como claim en la cookie de sesion
/// (UsuarioClaimsPrincipalFactory) y desaparece al renovar la sesion despues de cambiarla.
/// </summary>
public static class CambioContrasenaObligatorio
{
    public const string Ruta = "/cuenta/cambiar-contrasena";

    private static readonly string[] RutasPermitidas =
    [
        Ruta,
        "/cuenta/cerrar-sesion",
        "/cuenta/iniciar-sesion",
        "/_framework",
        "/_blazor",
        "/_content",
    ];

    public static IApplicationBuilder UseCambioContrasenaObligatorio(this IApplicationBuilder app)
        => app.Use(async (contexto, siguiente) =>
        {
            if (DebeCambiar(contexto) && !EsPermitida(contexto.Request.Path))
            {
                contexto.Response.Redirect(Ruta);
                return;
            }
            await siguiente(contexto);
        });

    public static bool DebeCambiar(HttpContext contexto)
        => contexto.User.Identity?.IsAuthenticated == true && contexto.User.HasClaim(ClaimsAsada.DebeCambiarContrasena, "1");

    private static bool EsPermitida(PathString ruta)
        => RutasPermitidas.Any(r => ruta.StartsWithSegments(r, StringComparison.OrdinalIgnoreCase));
}
