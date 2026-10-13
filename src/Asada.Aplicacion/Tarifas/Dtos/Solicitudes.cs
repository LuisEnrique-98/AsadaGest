using Asada.Dominio.Enums;
using Asada.Dominio.Reglas;

namespace Asada.Aplicacion.Tarifas.Dtos;

public sealed record CrearCategoriaSolicitud(string? Nombre, string? Descripcion);

/// <param name="Tramos">Tramos en orden; el ultimo sin limite.</param>
public sealed record CrearTarifaSolicitud(
    int CategoriaId,
    DateOnly VigenteDesde,
    decimal CargoFijo,
    ModoHidrante ModoHidrante,
    decimal ValorHidrante,
    string? Observaciones,
    IReadOnlyList<TramoTarifa> Tramos);

public sealed record AsignarCategoriaSolicitud(int ServicioId, int CategoriaId);
