using Asada.Dominio.Enums;

namespace Asada.Aplicacion.Mediciones.Dtos;

public sealed record MedidorDto(
    int Id,
    string NumeroSerie,
    DateOnly FechaInstalacion,
    decimal LecturaInicial,
    EstadoMedidor Estado,
    DateOnly? FechaRetiro,
    decimal? LecturaFinal,
    string? Observaciones);

public sealed record ServicioMedidoresDto(
    int ServicioId,
    int AbonadoId,
    string AbonadoCodigo,
    string AbonadoNombre,
    string PropiedadDireccion,
    EstadoRegistro EstadoServicio,
    IReadOnlyList<MedidorDto> Medidores)
{
    public MedidorDto? MedidorActivo => Medidores.FirstOrDefault(m => m.Estado == EstadoMedidor.Activo);
}

public sealed record LecturaRegistradaDto(
    int MedicionId,
    decimal Lectura,
    decimal Consumo,
    bool RequiereRevision,
    IReadOnlyList<string> MotivosRevision);

/// <summary>Una fila del listado de un periodo: un servicio activo, con o sin lectura.</summary>
public sealed record FilaLecturaPeriodoDto(
    int ServicioId,
    int AbonadoId,
    string AbonadoCodigo,
    string AbonadoNombre,
    string PropiedadDireccion,
    string? NumeroSerieMedidor,
    // Lectura del periodo (null si todavia no se tomo):
    DateOnly? FechaLectura,
    decimal? Lectura,
    decimal? Consumo,
    bool RequiereRevision,
    string? MotivosRevision,
    // Anotacion del operador (null si no hay):
    DecisionSeguimientoLectura? Decision,
    string? NotaSeguimiento)
{
    public bool TieneLectura => Lectura.HasValue;
    public bool TieneMedidor => NumeroSerieMedidor is not null;
}

public sealed record LecturasPeriodoDto(
    int Anio,
    int Mes,
    DateOnly InicioVentana,
    DateOnly FinVentana,
    EstadoVentanaLectura EstadoVentana,
    IReadOnlyList<FilaLecturaPeriodoDto> Filas)
{
    public int Total => Filas.Count;
    public int ConLectura => Filas.Count(f => f.TieneLectura);
    public int SinLectura => Filas.Count(f => !f.TieneLectura);
    public int ParaRevision => Filas.Count(f => f.RequiereRevision);

    /// <summary>Hay alerta cuando la ventana ya paso y quedan servicios sin lectura.</summary>
    public bool HayAlerta => EstadoVentana == EstadoVentanaLectura.Cerrada && SinLectura > 0;
}
