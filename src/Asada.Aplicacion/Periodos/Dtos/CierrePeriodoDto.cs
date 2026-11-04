using Asada.Dominio.Enums;

namespace Asada.Aplicacion.Periodos.Dtos;

/// <summary>Situacion de un periodo frente a su cierre.</summary>
public sealed record CierrePeriodoDto(
    int Anio,
    int Mes,
    EstadoPeriodo Estado,
    EstadoVentanaLectura EstadoVentana,
    DateOnly FinVentana,
    // Ultimo cierre y reapertura:
    DateTime? FechaCierre,
    string? CerradoPorNombre,
    DateTime? FechaReapertura,
    string? ReabiertoPorNombre,
    string? MotivoReapertura,
    // Resumen de la facturacion del periodo:
    int Servicios,
    int ConRecibo,
    decimal MontoEmitido,
    int SinLectura,
    int ConLecturaSinRecibo,
    int Anulados,
    /// <summary>Lo que impide cerrar el periodo (vacio si se puede cerrar).</summary>
    IReadOnlyList<string> Impedimentos)
{
    public bool EstaCerrado => Estado == EstadoPeriodo.Cerrado;
    public bool PuedeCerrarse => !EstaCerrado && Impedimentos.Count == 0;
}

public sealed record ReabrirPeriodoSolicitud(int Anio, int Mes, string Motivo);
