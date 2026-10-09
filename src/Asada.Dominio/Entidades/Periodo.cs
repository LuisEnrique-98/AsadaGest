using Asada.Dominio.Enums;
using Asada.Dominio.Reglas;

namespace Asada.Dominio.Entidades;

/// <summary>Mes de facturacion de una organizacion. Se crea la primera vez que se usa.</summary>
public class Periodo
{
    public int Id { get; set; }
    public int OrganizacionId { get; set; }
    public int Anio { get; set; }
    public int Mes { get; set; }
    public EstadoPeriodo Estado { get; set; } = EstadoPeriodo.Abierto;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public DateOnly InicioVentanaLectura => ReglasPeriodo.InicioVentana(Anio, Mes);
    public DateOnly FinVentanaLectura => ReglasPeriodo.FinVentana(Anio, Mes);
}
