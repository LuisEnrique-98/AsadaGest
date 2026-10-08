using Asada.Dominio.Enums;

namespace Asada.Aplicacion.Abonados.Dtos;

public sealed record CrearAbonadoSolicitud(
    TipoIdentificacion TipoIdentificacion,
    string Identificacion,
    string Nombre,
    string? Telefono,
    string? Correo,
    string? Direccion);

public sealed record AgregarPropiedadSolicitud(
    int AbonadoId,
    string Direccion,
    string? Provincia,
    string? Canton,
    string? Distrito,
    string? Referencia);

public sealed record AgregarServicioSolicitud(
    int AbonadoId,
    int PropiedadId,
    DateOnly FechaInicio,
    string? Observaciones,
    bool EsMoroso,
    int MensualidadesPendientes,
    decimal MontoPendiente);
