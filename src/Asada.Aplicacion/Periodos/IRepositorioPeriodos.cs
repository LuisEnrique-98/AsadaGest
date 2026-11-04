using Asada.Dominio.Entidades;

namespace Asada.Aplicacion.Periodos;

public interface IRepositorioPeriodos
{
    /// <summary>Periodo existente (con seguimiento de cambios), o null.</summary>
    Task<Periodo?> ObtenerPeriodoAsync(int anio, int mes, CancellationToken ct = default);

    /// <summary>Devuelve el periodo, creandolo si todavia no existe.</summary>
    Task<Periodo> ObtenerOCrearPeriodoAsync(int anio, int mes, CancellationToken ct = default);

    Task GuardarCambiosAsync(CancellationToken ct = default);
}
