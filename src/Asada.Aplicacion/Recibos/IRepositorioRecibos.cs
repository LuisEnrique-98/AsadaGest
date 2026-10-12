using Asada.Dominio.Entidades;

namespace Asada.Aplicacion.Recibos;

/// <summary>
/// Lo que la facturacion necesita saber de cada servicio en un periodo. Los datos vienen ya
/// juntos de varias tablas (abonado, propiedad, categoria, lectura, medidor, recibo).
/// </summary>
public sealed record ServicioAFacturar(
    int ServicioId,
    int AbonadoId,
    string AbonadoCodigo,
    string AbonadoNombre,
    string PropiedadDireccion,
    int? CategoriaId,
    string? CategoriaNombre,
    // Lectura del periodo (null si no hay):
    int? MedicionId,
    decimal? Lectura,
    DateOnly? FechaLectura,
    decimal? Consumo,
    bool LecturaRequiereRevision,
    string? MotivosRevision,
    string? NumeroSerieMedidor,
    int? MedidorId,
    // Lectura anterior del servicio (null si es la primera):
    decimal? LecturaAnterior,
    int? MedidorAnteriorId,
    // Recibo emitido del periodo (null si no hay):
    int? ReciboId,
    string? ReciboNumero,
    decimal? ReciboTotal);

public interface IRepositorioRecibos
{
    /// <summary>
    /// Servicios del periodo: los activos (con propiedad y abonado activos) que ya existian al
    /// cerrar el periodo, mas cualquier servicio que ya tenga recibo emitido en ese periodo.
    /// Ordenados por nombre del abonado.
    /// </summary>
    Task<IReadOnlyList<ServicioAFacturar>> ListarServiciosDelPeriodoAsync(int anio, int mes, CancellationToken ct = default);

    /// <summary>Todas las versiones de tarifa de la organizacion, con tramos y categoria.</summary>
    Task<IReadOnlyList<Tarifa>> ListarTarifasAsync(CancellationToken ct = default);

    Task<Periodo?> ObtenerPeriodoAsync(int anio, int mes, CancellationToken ct = default);

    /// <summary>
    /// Guarda todos los recibos en una sola transaccion, asignando a cada uno su correlativo y su
    /// numero (CB-REC-anio-000001) de forma atomica. Devuelve false, sin guardar ninguno, si otro
    /// usuario genero al mismo tiempo un recibo para alguno de esos servicios y periodo.
    /// </summary>
    Task<bool> AgregarRecibosAsync(int anio, IReadOnlyList<Recibo> recibos, CancellationToken ct = default);

    /// <summary>Recibo con su desglose y su periodo, o null.</summary>
    Task<(Recibo Recibo, Periodo Periodo)?> ObtenerReciboAsync(int reciboId, CancellationToken ct = default);

    /// <summary>Organizacion actual (datos del encabezado del recibo).</summary>
    Task<Organizacion?> ObtenerOrganizacionAsync(int organizacionId, CancellationToken ct = default);
}
