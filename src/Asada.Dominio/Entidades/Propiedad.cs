using Asada.Dominio.Enums;

namespace Asada.Dominio.Entidades;

/// <summary>Inmueble (casa, local, lote) de un abonado. Puede tener uno o mas servicios.</summary>
public class Propiedad
{
    public int Id { get; set; }
    public int OrganizacionId { get; set; }

    public int AbonadoId { get; set; }
    public Abonado? Abonado { get; set; }

    public required string Direccion { get; set; }
    public string? Provincia { get; set; }
    public string? Canton { get; set; }
    public string? Distrito { get; set; }

    /// <summary>Senas o referencias para ubicar el inmueble.</summary>
    public string? Referencia { get; set; }

    public EstadoRegistro Estado { get; set; } = EstadoRegistro.Activo;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public List<Servicio> Servicios { get; set; } = [];

    public Servicio AgregarServicio(
        DateOnly fechaInicio, string? observaciones = null,
        bool esMoroso = false, int mensualidadesPendientes = 0, decimal montoPendiente = 0m)
    {
        var servicio = new Servicio
        {
            OrganizacionId = OrganizacionId,
            Propiedad = this,
            FechaInicio = fechaInicio,
            Observaciones = observaciones,
            EsMoroso = esMoroso,
            MensualidadesPendientes = mensualidadesPendientes,
            MontoPendiente = montoPendiente,
        };
        Servicios.Add(servicio);
        return servicio;
    }
}
