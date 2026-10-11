using Asada.Aplicacion.Abonados.Dtos;
using Asada.Aplicacion.Common;
using Asada.Dominio.Entidades;
using Asada.Dominio.Enums;

namespace Asada.Aplicacion.Abonados;

/// <summary>
/// Acceso a datos del agregado Abonado -> Propiedades -> Servicios. La implementacion
/// (Asada.Infraestructura) pasa siempre por AsadaDbContext, asi que el filtro global de
/// organizacion (RB-015) aplica a todos estos metodos sin que el caso de uso lo recuerde.
/// </summary>
public interface IRepositorioAbonados
{
    /// <summary>True si ya existe un abonado de la organizacion actual con esa identificacion (ya normalizada).</summary>
    /// <param name="excluirAbonadoId">Al editar, el propio abonado no cuenta como duplicado.</param>
    Task<bool> ExisteIdentificacionAsync(TipoIdentificacion tipo, string identificacionNormalizada, int? excluirAbonadoId = null, CancellationToken ct = default);

    /// <summary>Guarda el abonado nuevo y le asigna, de forma atomica, su correlativo y su codigo (CB-AB-000125).</summary>
    Task AgregarAsync(Abonado abonado, CancellationToken ct = default);

    /// <summary>Abonado con sus propiedades y servicios, listo para modificarse; null si no existe o es de otra organizacion.</summary>
    Task<Abonado?> ObtenerConExpedienteAsync(int abonadoId, CancellationToken ct = default);

    Task<PaginaResultado<AbonadoResumenDto>> BuscarAsync(string? texto, int pagina, int tamanoPagina, CancellationToken ct = default);

    Task GuardarCambiosAsync(CancellationToken ct = default);
}
