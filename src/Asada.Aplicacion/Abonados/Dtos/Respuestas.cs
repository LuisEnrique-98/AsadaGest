using Asada.Dominio.Enums;

namespace Asada.Aplicacion.Abonados.Dtos;

public sealed record AbonadoResumenDto(
    int Id,
    string Codigo,
    string Nombre,
    TipoIdentificacion TipoIdentificacion,
    string Identificacion,
    string? Telefono,
    EstadoRegistro Estado,
    int CantidadServicios);

public sealed record ServicioDto(
    int Id,
    DateOnly FechaInicio,
    EstadoRegistro Estado,
    string? Observaciones,
    bool EsMoroso,
    int MensualidadesPendientes,
    decimal MontoPendiente);

public sealed record PropiedadDto(
    int Id,
    string Direccion,
    string? Provincia,
    string? Canton,
    string? Distrito,
    string? Referencia,
    EstadoRegistro Estado,
    IReadOnlyList<ServicioDto> Servicios);

public sealed record ExpedienteAbonadoDto(
    int Id,
    string Codigo,
    TipoIdentificacion TipoIdentificacion,
    string Identificacion,
    string Nombre,
    string? Telefono,
    string? Correo,
    string? Direccion,
    EstadoRegistro Estado,
    DateTime FechaCreacion,
    IReadOnlyList<PropiedadDto> Propiedades);
