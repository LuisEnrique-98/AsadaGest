using Asada.Aplicacion.Common;
using Asada.Aplicacion.Recibos;
using Asada.Aplicacion.Recibos.Dtos;

namespace Asada.Presentacion;

/// <summary>
/// Descarga de recibos en PDF. Son endpoints HTTP normales (no paginas Blazor) porque devuelven
/// un archivo. El aislamiento por ASADA lo garantiza el HasQueryFilter de la base de datos: un
/// usuario nunca obtiene el PDF de un recibo de otra organizacion.
/// </summary>
public static class EndpointsRecibos
{
    public static IEndpointRouteBuilder MapearEndpointsRecibos(this IEndpointRouteBuilder app)
    {
        var recibos = app.MapGroup("/recibos").RequireAuthorization();

        // PDF de un recibo: /recibos/15/pdf
        recibos.MapGet("/{reciboId:int}/pdf", async (int reciboId, DescargarPdfReciboCasoUso casoUso, HttpContext http, CancellationToken ct)
            => Responder(await casoUso.EjecutarAsync(reciboId, ct), http, StatusCodes.Status404NotFound));

        // Todos los recibos emitidos del periodo, dos por hoja carta: /recibos/pdf?anio=2026&mes=10
        recibos.MapGet("/pdf", async (int anio, int mes, DescargarPdfRecibosPeriodoCasoUso casoUso, HttpContext http, CancellationToken ct)
            => Responder(await casoUso.EjecutarAsync(anio, mes, ct), http, StatusCodes.Status400BadRequest));

        return app;
    }

    /// <summary>
    /// El PDF se abre en el visor del navegador ("inline") para revisarlo e imprimirlo; si el
    /// usuario lo guarda, el archivo conserva su nombre (p. ej. CB-REC-2026-000015.pdf).
    /// </summary>
    private static IResult Responder(Resultado<ArchivoDescarga> resultado, HttpContext http, int codigoError)
    {
        if (!resultado.EsExitoso)
            return Results.Text(string.Join(" ", resultado.Errores), "text/plain; charset=utf-8", statusCode: codigoError);

        var archivo = resultado.Valor!;
        http.Response.Headers.ContentDisposition = $"inline; filename=\"{archivo.NombreArchivo}\"";
        return Results.File(archivo.Contenido, archivo.TipoContenido);
    }
}
