namespace Asada.Infraestructura.Persistence;

/// <summary>
/// Contador de recibos por organizacion y anio. Es un detalle de persistencia (no un concepto
/// de negocio), por eso vive en Infraestructura. Solo se lee y escribe con SQL directo dentro
/// de la transaccion que guarda los recibos (ver RepositorioRecibos.AgregarRecibosAsync).
/// </summary>
public class CorrelativoRecibo
{
    public int OrganizacionId { get; set; }
    public int Anio { get; set; }
    public int Ultimo { get; set; }
}
