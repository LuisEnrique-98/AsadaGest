using Asada.Dominio.Entidades;

namespace Asada.Aplicacion.Tarifas;

/// <summary>
/// Acceso a datos de categorias y tarifas. Pasa por AsadaDbContext, asi que el filtro de
/// organizacion (RB-015) se aplica solo: nunca devuelve categorias ni tarifas de otra ASADA.
/// </summary>
public interface IRepositorioTarifas
{
    /// <summary>Categorias de la organizacion ordenadas por nombre.</summary>
    Task<IReadOnlyList<CategoriaServicio>> ListarCategoriasAsync(CancellationToken ct = default);

    /// <summary>Cantidad de servicios activos por categoria (clave = Id de la categoria).</summary>
    Task<IReadOnlyDictionary<int, int>> ContarServiciosPorCategoriaAsync(CancellationToken ct = default);

    Task<int> ContarServiciosActivosSinCategoriaAsync(CancellationToken ct = default);

    Task<CategoriaServicio?> ObtenerCategoriaAsync(int categoriaId, CancellationToken ct = default);
    Task<bool> ExisteCategoriaConNombreAsync(string nombre, CancellationToken ct = default);
    Task AgregarCategoriaAsync(CategoriaServicio categoria, CancellationToken ct = default);

    /// <summary>Todas las versiones de tarifa de la organizacion, con sus tramos.</summary>
    Task<IReadOnlyList<Tarifa>> ListarTarifasAsync(CancellationToken ct = default);

    /// <summary>Tarifa con sus tramos y su categoria, o null.</summary>
    Task<Tarifa?> ObtenerTarifaAsync(int tarifaId, CancellationToken ct = default);

    Task AgregarTarifaAsync(Tarifa tarifa, CancellationToken ct = default);

    /// <summary>Ultimo periodo (anio, mes) con recibos emitidos con alguna tarifa de la categoria, o null.</summary>
    Task<(int Anio, int Mes)?> ObtenerUltimoPeriodoFacturadoAsync(int categoriaId, CancellationToken ct = default);

    /// <summary>Servicio con su propiedad y abonado, o null.</summary>
    Task<Servicio?> ObtenerServicioAsync(int servicioId, CancellationToken ct = default);

    Task GuardarCambiosAsync(CancellationToken ct = default);
}
