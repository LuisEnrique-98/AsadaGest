using Asada.Dominio.Enums;

namespace Asada.Dominio.Entidades;

/// <summary>
/// Medidor instalado en un servicio. Un servicio tiene a lo sumo un medidor Activo a la vez,
/// pero conserva todo el historial de medidores retirados: ese historial es lo que permite
/// calcular bien el consumo cuando se cambia un medidor (RB-013, ver CalculadoraConsumo).
/// </summary>
public class Medidor
{
    public int Id { get; set; }
    public int OrganizacionId { get; set; }
    public int ServicioId { get; set; }

    public required string NumeroSerie { get; set; }
    public DateOnly FechaInstalacion { get; set; }

    /// <summary>Lectura que marcaba el medidor al instalarse (un medidor nuevo suele empezar en 0).</summary>
    public decimal LecturaInicial { get; set; }

    public EstadoMedidor Estado { get; private set; } = EstadoMedidor.Activo;
    public DateOnly? FechaRetiro { get; private set; }

    /// <summary>Lectura que marcaba al retirarse. Es obligatoria para poder calcular el consumo del tramo.</summary>
    public decimal? LecturaFinal { get; private set; }

    public string? Observaciones { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public void Retirar(DateOnly fecha, decimal lecturaFinal)
    {
        if (Estado != EstadoMedidor.Activo)
            throw new InvalidOperationException("El medidor ya fue retirado.");
        if (lecturaFinal < LecturaInicial)
            throw new InvalidOperationException("La lectura final no puede ser menor que la lectura inicial.");
        if (fecha < FechaInstalacion)
            throw new InvalidOperationException("La fecha de retiro no puede ser anterior a la de instalacion.");

        Estado = EstadoMedidor.Retirado;
        FechaRetiro = fecha;
        LecturaFinal = lecturaFinal;
    }
}
