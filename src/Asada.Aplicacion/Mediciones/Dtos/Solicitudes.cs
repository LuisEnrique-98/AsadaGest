using Asada.Dominio.Enums;

namespace Asada.Aplicacion.Mediciones.Dtos;

public sealed record InstalarMedidorSolicitud(
    int ServicioId,
    string NumeroSerie,
    DateOnly FechaInstalacion,
    decimal LecturaInicial,
    string? Observaciones);

public sealed record RetirarMedidorSolicitud(
    int MedidorId,
    DateOnly FechaRetiro,
    decimal LecturaFinal);

public sealed record RegistrarLecturaSolicitud(
    int ServicioId,
    int Anio,
    int Mes,
    DateOnly FechaLectura,
    decimal Lectura,
    string? Observaciones);

public sealed record RegistrarSeguimientoLecturaSolicitud(
    int ServicioId,
    int Anio,
    int Mes,
    DecisionSeguimientoLectura Decision,
    string? Nota);
