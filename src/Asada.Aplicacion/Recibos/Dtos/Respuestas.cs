using Asada.Dominio.Enums;

namespace Asada.Aplicacion.Recibos.Dtos;

/// <summary>Situacion de un servicio frente a la facturacion de un periodo.</summary>
public enum EstadoFacturacion
{
    /// <summary>Ya tiene recibo emitido en el periodo.</summary>
    ConRecibo = 1,
    /// <summary>Tiene lectura, categoria y tarifa: se le puede generar el recibo.</summary>
    ListoParaFacturar = 2,
    /// <summary>No tiene lectura del periodo: no se le genera recibo hasta que la tenga.</summary>
    SinLectura = 3,
    /// <summary>El servicio no tiene categoria tarifaria asignada.</summary>
    SinCategoria = 4,
    /// <summary>Su categoria no tiene una tarifa que rija para el periodo.</summary>
    SinTarifa = 5,
}

public sealed record FilaFacturacionDto(
    int ServicioId,
    int AbonadoId,
    string AbonadoCodigo,
    string AbonadoNombre,
    string PropiedadDireccion,
    string? CategoriaNombre,
    decimal? Consumo,
    bool LecturaRequiereRevision,
    EstadoFacturacion Estado,
    int? ReciboId,
    string? ReciboNumero,
    decimal? Total);

public sealed record FacturacionPeriodoDto(int Anio, int Mes, IReadOnlyList<FilaFacturacionDto> Filas)
{
    public int Total => Filas.Count;
    public int ConRecibo => Filas.Count(f => f.Estado == EstadoFacturacion.ConRecibo);
    public int Listos => Filas.Count(f => f.Estado == EstadoFacturacion.ListoParaFacturar);
    public int ListosParaRevision => Filas.Count(f => f.Estado == EstadoFacturacion.ListoParaFacturar && f.LecturaRequiereRevision);
    public int SinLectura => Filas.Count(f => f.Estado == EstadoFacturacion.SinLectura);
    public int SinCategoria => Filas.Count(f => f.Estado == EstadoFacturacion.SinCategoria);
    public int SinTarifa => Filas.Count(f => f.Estado == EstadoFacturacion.SinTarifa);
    public decimal MontoEmitido => Filas.Where(f => f.Estado == EstadoFacturacion.ConRecibo).Sum(f => f.Total ?? 0m);
}

public sealed record GeneracionRecibosDto(int Generados, int ParaRevision, decimal MontoGenerado, int Pendientes);

public sealed record LineaReciboDto(TipoLineaRecibo Tipo, string Concepto, decimal? Cantidad, decimal? PrecioUnitario, decimal Monto);

public sealed record ReciboDto(
    int Id,
    string Numero,
    EstadoRecibo Estado,
    int Anio,
    int Mes,
    DateOnly FechaEmision,
    DateOnly FechaVencimiento,
    // ASADA emisora
    string OrganizacionNombre,
    string? OrganizacionCedulaJuridica,
    string? OrganizacionTelefono,
    string? OrganizacionCorreo,
    // Servicio (datos congelados al emitir)
    int ServicioId,
    string AbonadoCodigo,
    string AbonadoNombre,
    string PropiedadDireccion,
    string CategoriaNombre,
    string NumeroSerieMedidor,
    decimal? LecturaAnterior,
    decimal LecturaActual,
    DateOnly FechaLectura,
    decimal Consumo,
    bool HuboCambioMedidor,
    bool LecturaRequiereRevision,
    string? MotivosRevision,
    IReadOnlyList<LineaReciboDto> Lineas,
    decimal SubtotalAgua,
    decimal MontoHidrante,
    decimal Total);

/// <summary>Archivo para descargar desde el navegador (p. ej. el PDF de los recibos).</summary>
public sealed record ArchivoDescarga(string NombreArchivo, string TipoContenido, byte[] Contenido)
{
    public static ArchivoDescarga Pdf(string nombreArchivo, byte[] contenido) => new(nombreArchivo, "application/pdf", contenido);
}
