using Asada.Aplicacion.Mediciones.Dtos;
using Asada.Dominio.Entidades;
using Asada.Dominio.Reglas;

namespace Asada.Aplicacion.Mediciones;

/// <summary>
/// Acceso a datos de medidores, periodos, lecturas y seguimientos. Como el resto de la capa de
/// datos, pasa siempre por AsadaDbContext, de modo que el filtro de organizacion (RB-015) aplica solo.
/// </summary>
/// <summary>Recibo emitido con una lectura: impide corregirla o anularla mientras no se anule.</summary>
public sealed record ReciboDeLectura(int Id, string Numero);

public interface IRepositorioMediciones
{
    /// <summary>Servicio con su propiedad y abonado, o null si no existe o es de otra organizacion.</summary>
    Task<Servicio?> ObtenerServicioAsync(int servicioId, CancellationToken ct = default);

    /// <summary>Medidores del servicio en orden de instalacion (el mas antiguo primero).</summary>
    Task<IReadOnlyList<Medidor>> ListarMedidoresDeServicioAsync(int servicioId, CancellationToken ct = default);

    Task<Medidor?> ObtenerMedidorAsync(int medidorId, CancellationToken ct = default);
    Task<bool> ExisteNumeroSerieAsync(string numeroSerie, CancellationToken ct = default);
    Task AgregarMedidorAsync(Medidor medidor, CancellationToken ct = default);

    /// <summary>Devuelve el periodo, creandolo si todavia no existe.</summary>
    Task<Periodo> ObtenerOCrearPeriodoAsync(int anio, int mes, CancellationToken ct = default);

    /// <summary>Periodo existente, o null (no lo crea).</summary>
    Task<Periodo?> ObtenerPeriodoAsync(int anio, int mes, CancellationToken ct = default);

    Task<Periodo?> ObtenerPeriodoPorIdAsync(int periodoId, CancellationToken ct = default);

    /// <summary>Umbral de consumo anormal de la ASADA del usuario (Organizacion.PorcentajeConsumoAnormal).</summary>
    Task<decimal> ObtenerPorcentajeConsumoAnormalAsync(CancellationToken ct = default);

    /// <summary>Ultima lectura vigente del servicio (las anuladas no cuentan).</summary>
    Task<LecturaAnterior?> ObtenerUltimaLecturaAsync(int servicioId, CancellationToken ct = default);

    /// <summary>Ultima lectura vigente del servicio en un periodo anterior al indicado.</summary>
    Task<LecturaAnterior?> ObtenerLecturaAnteriorAAsync(int servicioId, int anio, int mes, CancellationToken ct = default);

    /// <summary>Consumos de periodos anteriores al indicado, del mas reciente al mas antiguo.</summary>
    Task<IReadOnlyList<decimal>> ObtenerConsumosAnterioresAAsync(int servicioId, int anio, int mes, int cantidad, CancellationToken ct = default);

    /// <summary>Lectura vigente por id (para corregirla o anularla), o null.</summary>
    Task<Medicion?> ObtenerMedicionAsync(int medicionId, CancellationToken ct = default);

    /// <summary>Recibo emitido (no anulado) que se facturo con esta lectura, o null.</summary>
    Task<ReciboDeLectura?> ObtenerReciboEmitidoDeLecturaAsync(int medicionId, CancellationToken ct = default);

    /// <summary>Consumos previos del servicio, del mas reciente al mas antiguo.</summary>
    Task<IReadOnlyList<decimal>> ObtenerConsumosRecientesAsync(int servicioId, int cantidad, CancellationToken ct = default);

    Task<bool> ExisteLecturaAsync(int servicioId, int anio, int mes, CancellationToken ct = default);
    Task AgregarMedicionAsync(Medicion medicion, CancellationToken ct = default);

    Task<SeguimientoLectura?> ObtenerSeguimientoAsync(int servicioId, int periodoId, CancellationToken ct = default);
    Task AgregarSeguimientoAsync(SeguimientoLectura seguimiento, CancellationToken ct = default);

    /// <summary>Servicios activos de la organizacion, cada uno con la lectura y el seguimiento del periodo si los tiene.</summary>
    Task<IReadOnlyList<FilaLecturaPeriodoDto>> ListarFilasDelPeriodoAsync(int anio, int mes, CancellationToken ct = default);

    Task GuardarCambiosAsync(CancellationToken ct = default);
}
