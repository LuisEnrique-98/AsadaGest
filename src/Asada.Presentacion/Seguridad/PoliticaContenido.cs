namespace Asada.Presentacion.Seguridad;

/// <summary>
/// Sprint 6c (M-2): cabeceras que le dicen al navegador que puede cargar la pagina.
///
///  - Content-Security-Policy (CSP): solo scripts, estilos, imagenes y conexiones del propio sitio; nada
///    de JavaScript ni estilos escritos dentro del HTML (por eso las confirmaciones viven en
///    wwwroot/js/asada.js y los anchos en app.css). Si alguien lograra inyectar un script en una pagina,
///    el navegador no lo ejecutaria. Tampoco deja mostrar el sistema dentro de otro sitio (iframe).
///  - Permissions-Policy: el sistema no usa camara, microfono, ubicacion, pagos ni USB; se apagan.
///
/// La CSP tiene dos modos (Seguridad:Csp; en el servidor, CSP_MODO en despliegue/.env):
///    informe  (por defecto) el navegador no bloquea nada: solo avisa lo que bloquearia, y el aviso
///             queda en el log de la aplicacion ("Aviso de la politica de contenido").
///    aplicar  el navegador bloquea lo que no cumpla (y sigue avisando).
/// Se empieza en "informe", se usa el sistema unos dias revisando el log, y se pasa a "aplicar".
///
/// Los PDF no llevan la CSP: el visor de PDF de algunos navegadores no funciona con ella.
/// X-Content-Type-Options, X-Frame-Options y Referrer-Policy las pone Caddy (despliegue/Caddyfile).
/// </summary>
public static class PoliticaContenido
{
    public enum Modo
    {
        Informe,
        Aplicar,
    }

    public const string ClaveModo = "Seguridad:Csp";
    public const string RutaAviso = "/seguridad/csp-aviso";

    /// <summary>Tamano maximo que se lee (y se anota en el log) de cada aviso del navegador.</summary>
    public const int LargoMaximoAviso = 4 * 1024;

    public static readonly string Politica = string.Join("; ",
        "default-src 'self'",
        "script-src 'self'",
        "style-src 'self'",
        // Bootstrap y app.css dibujan algunos iconos con imagenes "data:" dentro del CSS.
        "img-src 'self' data:",
        "font-src 'self'",
        "connect-src 'self'",
        "object-src 'none'",
        "base-uri 'self'",
        "form-action 'self'",
        "frame-ancestors 'none'",
        "report-uri " + RutaAviso);

    public const string PermisosNavegador = "camera=(), microphone=(), geolocation=(), payment=(), usb=()";

    /// <summary>"aplicar" (sin importar mayusculas) aplica la politica; cualquier otro valor, o ninguno, es "informe".</summary>
    public static Modo LeerModo(string? valor)
        => string.Equals(valor?.Trim(), "aplicar", StringComparison.OrdinalIgnoreCase) ? Modo.Aplicar : Modo.Informe;

    public static string NombreCabecera(Modo modo)
        => modo == Modo.Aplicar ? "Content-Security-Policy" : "Content-Security-Policy-Report-Only";

    public static WebApplication UsePoliticaContenido(this WebApplication app)
    {
        var modo = LeerModo(app.Configuration[ClaveModo]);
        var cabecera = NombreCabecera(modo);
        app.Logger.LogInformation("Politica de contenido (CSP) en modo {Modo}.", modo == Modo.Aplicar ? "aplicar" : "informe");

        app.Use(async (contexto, siguiente) =>
        {
            contexto.Response.OnStarting(() =>
            {
                var cabeceras = contexto.Response.Headers;
                cabeceras["Permissions-Policy"] = PermisosNavegador;
                if (!EsPdf(contexto.Response.ContentType))
                    cabeceras[cabecera] = Politica;
                return Task.CompletedTask;
            });
            await siguiente(contexto);
        });
        return app;
    }

    public static bool EsPdf(string? tipoContenido)
        => tipoContenido is not null && tipoContenido.StartsWith("application/pdf", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// El navegador manda aqui un aviso (JSON) por cada cosa que la CSP bloquea o bloquearia. Es publico
    /// (el aviso puede llegar desde la pagina de inicio de sesion) y tiene tope por IP (LimitesSolicitudes).
    /// </summary>
    public static IEndpointRouteBuilder MapearAvisoCsp(this IEndpointRouteBuilder app)
    {
        app.MapPost(RutaAviso, async (HttpContext contexto, ILoggerFactory logs) =>
            {
                var texto = await LeerAvisoAsync(contexto.Request.Body, contexto.RequestAborted);
                logs.CreateLogger("Asada.Seguridad.Csp").LogWarning("Aviso de la politica de contenido (CSP): {Aviso}", texto);
                return Results.NoContent();
            })
            .AllowAnonymous()
            .DisableAntiforgery();
        return app;
    }

    /// <summary>Lee a lo sumo LargoMaximoAviso caracteres y cambia los saltos de linea y otros caracteres de control por espacios.</summary>
    public static async Task<string> LeerAvisoAsync(Stream cuerpo, CancellationToken ct)
    {
        using var lector = new StreamReader(cuerpo);
        var bufer = new char[LargoMaximoAviso];
        var leidos = await lector.ReadBlockAsync(bufer.AsMemory(), ct);
        for (var i = 0; i < leidos; i++)
        {
            if (char.IsControl(bufer[i])) bufer[i] = ' ';
        }
        return new string(bufer, 0, leidos);
    }
}
