using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Asada.Presentacion.Seguridad;

/// <summary>
/// Valores de los limites de solicitudes (Sprint 6c). Se pueden cambiar sin recompilar con la seccion
/// "Seguridad:Limites" de la configuracion; en el servidor, con variables de despliegue/.env (ver
/// docker-compose.yml).
/// </summary>
public sealed class OpcionesLimites
{
    public const string Seccion = "Seguridad:Limites";

    /// <summary>Intentos de iniciar sesion o cambiar la contrasena por minuto, desde una misma IP.</summary>
    public int IngresosPorMinuto { get; set; } = 10;

    /// <summary>Intentos de iniciar sesion o cambiar la contrasena por hora, desde una misma IP.</summary>
    public int IngresosPorHora { get; set; } = 30;

    /// <summary>PDF del periodo completo por minuto, por usuario (ademas, uno a la vez).</summary>
    public int PdfPeriodoPorMinuto { get; set; } = 10;

    /// <summary>Avisos de la politica de contenido (CSP) por minuto, desde una misma IP.</summary>
    public int AvisosCspPorMinuto { get; set; } = 20;
}

/// <summary>
/// Sprint 6c: limites de solicitudes con el limitador que trae ASP.NET Core.
///
///  - A-3: el login y el cambio de contrasena (solo el POST, es decir, cada intento) tienen un tope
///    por IP: 10 por minuto y 30 por hora (decision del 20/11/2026). El bloqueo de Identity (5 intentos
///    fallidos -> 15 minutos) protege una cuenta; este tope frena a quien prueba muchas cuentas desde
///    la misma conexion. Ojo: en la oficina todos salen por la misma IP; si el tope molesta, se sube
///    en despliegue/.env sin recompilar.
///  - B-3: el PDF de todos los recibos del periodo es pesado (se genera en el momento). Cada usuario
///    puede pedir uno a la vez y hasta 10 por minuto.
///  - El aviso de la CSP (PoliticaContenido) tambien tiene tope por IP, para que no llene el log.
///
/// Al pasarse del tope se responde 429 con una pagina sencilla y queda una advertencia en el log.
/// Detras de Caddy la IP es la del visitante (cabecera X-Forwarded-For, ver Dockerfile). Los limites
/// viven en la memoria de la aplicacion: se reinician al reiniciar el contenedor.
/// </summary>
public static class LimitesSolicitudes
{
    public enum Tipo
    {
        Ninguno,
        Credenciales,
        PdfPeriodo,
        AvisoCsp,
    }

    public const string RutaIniciarSesion = "/cuenta/iniciar-sesion";
    public const string RutaPdfPeriodo = "/recibos/pdf";

    private static readonly string[] RutasCredenciales = [RutaIniciarSesion, CambioContrasenaObligatorio.Ruta];

    public static IServiceCollection AgregarLimitesSolicitudes(this IServiceCollection servicios, IConfiguration configuracion)
    {
        var opciones = configuracion.GetSection(OpcionesLimites.Seccion).Get<OpcionesLimites>() ?? new OpcionesLimites();
        servicios.AddRateLimiter(limitador =>
        {
            limitador.GlobalLimiter = CrearLimitador(opciones);
            limitador.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limitador.OnRejected = ResponderRechazoAsync;
        });
        return servicios;
    }

    /// <summary>Que limite le toca a una solicitud. Todo lo demas (paginas, archivos, etc.) no tiene limite.</summary>
    public static Tipo Clasificar(HttpRequest solicitud)
    {
        // "/cuenta/iniciar-sesion/" tambien llega a la pagina: se compara sin la barra final.
        var ruta = (solicitud.Path.Value ?? "").TrimEnd('/');

        if (HttpMethods.IsPost(solicitud.Method))
        {
            if (RutasCredenciales.Any(r => string.Equals(ruta, r, StringComparison.OrdinalIgnoreCase)))
                return Tipo.Credenciales;
            if (string.Equals(ruta, PoliticaContenido.RutaAviso, StringComparison.OrdinalIgnoreCase))
                return Tipo.AvisoCsp;
        }
        else if (HttpMethods.IsGet(solicitud.Method)
                 && string.Equals(ruta, RutaPdfPeriodo, StringComparison.OrdinalIgnoreCase))
        {
            return Tipo.PdfPeriodo;
        }

        return Tipo.Ninguno;
    }

    /// <summary>
    /// Dos limitadores encadenados: una solicitud pasa solo si los dos la dejan. El primero cuenta por
    /// minuto; el segundo, por hora (credenciales) o de a uno a la vez (PDF).
    /// </summary>
    public static PartitionedRateLimiter<HttpContext> CrearLimitador(OpcionesLimites opciones)
        => PartitionedRateLimiter.CreateChained(
            PartitionedRateLimiter.Create<HttpContext, string>(contexto => Clasificar(contexto.Request) switch
            {
                Tipo.Credenciales => Ventana("ingreso-minuto|" + ClaveIp(contexto), opciones.IngresosPorMinuto, TimeSpan.FromMinutes(1)),
                Tipo.PdfPeriodo => Ventana("pdf-minuto|" + ClaveUsuario(contexto), opciones.PdfPeriodoPorMinuto, TimeSpan.FromMinutes(1)),
                Tipo.AvisoCsp => Ventana("csp-minuto|" + ClaveIp(contexto), opciones.AvisosCspPorMinuto, TimeSpan.FromMinutes(1)),
                _ => RateLimitPartition.GetNoLimiter(""),
            }),
            PartitionedRateLimiter.Create<HttpContext, string>(contexto => Clasificar(contexto.Request) switch
            {
                Tipo.Credenciales => Ventana("ingreso-hora|" + ClaveIp(contexto), opciones.IngresosPorHora, TimeSpan.FromHours(1)),
                Tipo.PdfPeriodo => RateLimitPartition.GetConcurrencyLimiter("pdf-simultaneo|" + ClaveUsuario(contexto),
                    _ => new ConcurrencyLimiterOptions { PermitLimit = 1, QueueLimit = 0 }),
                _ => RateLimitPartition.GetNoLimiter(""),
            }));

    private static RateLimitPartition<string> Ventana(string clave, int permitidos, TimeSpan ventana)
        => RateLimitPartition.GetFixedWindowLimiter(clave, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = Math.Max(1, permitidos),
            Window = ventana,
            QueueLimit = 0,
            AutoReplenishment = true,
        });

    /// <summary>
    /// IP del visitante. Una IPv6 se agrupa por su red (/64): quien tiene una conexion IPv6 puede usar
    /// muchas direcciones de esa red y asi no se salta el tope cambiando de direccion.
    /// </summary>
    public static string ClaveIp(HttpContext contexto)
    {
        var ip = contexto.Connection.RemoteIpAddress;
        if (ip is null) return "desconocida";
        if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();
        if (ip.AddressFamily != AddressFamily.InterNetworkV6) return ip.ToString();

        var bytes = ip.GetAddressBytes();
        Array.Clear(bytes, 8, 8);
        return new IPAddress(bytes) + "/64";
    }

    /// <summary>Usuario autenticado; sin sesion, la IP (la solicitud igual sera rechazada por no tener permiso).</summary>
    private static string ClaveUsuario(HttpContext contexto)
        => contexto.User.FindFirst(ClaimTypes.NameIdentifier)?.Value is { Length: > 0 } id
            ? "u" + id
            : "ip" + ClaveIp(contexto);

    private static async ValueTask ResponderRechazoAsync(OnRejectedContext rechazo, CancellationToken ct)
    {
        var contexto = rechazo.HttpContext;
        var tipo = Clasificar(contexto.Request);

        var log = contexto.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Asada.Seguridad.Limites");
        if (tipo == Tipo.AvisoCsp)
        {
            log.LogDebug("Avisos de la CSP descartados por exceso desde {Ip}.", ClaveIp(contexto));
            return;
        }
        log.LogWarning(
            "Limite de solicitudes alcanzado ({Tipo}) en {Ruta} desde {Ip}, usuario {Usuario}.",
            tipo, contexto.Request.Path.Value, ClaveIp(contexto),
            contexto.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "sin sesion");

        if (rechazo.Lease.TryGetMetadata(MetadataName.RetryAfter, out var espera))
            contexto.Response.Headers.RetryAfter = Math.Ceiling(espera.TotalSeconds).ToString(CultureInfo.InvariantCulture);

        contexto.Response.ContentType = "text/html; charset=utf-8";
        await contexto.Response.WriteAsync(PaginaRechazo(tipo), ct);
    }

    public static string PaginaRechazo(Tipo tipo)
    {
        var (mensaje, enlace, textoEnlace) = tipo == Tipo.PdfPeriodo
            ? ("Ya se esta generando un PDF de recibos o se pidieron muchos en poco tiempo. Espere un momento y vuelva a intentarlo.",
               "/recibos", "Volver a Recibos")
            : ("Se hicieron demasiados intentos desde esta conexion. Espere unos minutos y vuelva a intentarlo. " +
               "Si trabaja en la oficina, puede que otra persona de la misma red tambien este intentando entrar.",
               "/", "Volver al inicio");

        return $"""
            <!DOCTYPE html>
            <html lang="es">
            <head>
                <meta charset="utf-8" />
                <meta name="viewport" content="width=device-width, initial-scale=1.0" />
                <title>Demasiadas solicitudes</title>
                <link rel="stylesheet" href="/bootstrap/bootstrap.min.css" />
            </head>
            <body>
                <main class="container mt-5">
                    <h1 class="h3">Demasiadas solicitudes</h1>
                    <p>{mensaje}</p>
                    <p><a href="{enlace}">{textoEnlace}</a></p>
                </main>
            </body>
            </html>
            """;
    }
}
