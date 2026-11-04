using System.Text.Json;
using Asada.Aplicacion.Common;
using Asada.Dominio.Enums;

namespace Asada.Aplicacion.Auditoria;

/// <summary>Un campo cambiado: valor antes y despues (texto; null si no tenia valor).</summary>
public sealed record CambioAuditoria(string Campo, string? Antes, string? Despues)
{
    private static readonly JsonSerializerOptions Opciones = new();

    public static string Serializar(IEnumerable<CambioAuditoria> cambios) => JsonSerializer.Serialize(cambios, Opciones);

    public static IReadOnlyList<CambioAuditoria> Deserializar(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return JsonSerializer.Deserialize<List<CambioAuditoria>>(json, Opciones) ?? []; }
        catch (JsonException) { return []; }
    }
}

public sealed record RegistroAuditoriaDto(
    long Id,
    DateTime Fecha,
    string UsuarioNombre,
    AccionAuditoria Accion,
    string Entidad,
    long EntidadId,
    IReadOnlyList<CambioAuditoria> Cambios);

public sealed record FiltroAuditoria(
    DateOnly? Desde,
    DateOnly? Hasta,
    string? Entidad,
    long? EntidadId,
    string? Usuario);

public interface IRepositorioAuditoria
{
    /// <summary>
    /// Registros de la ASADA del usuario (filtro de organizacion), del mas reciente al mas antiguo.
    /// Desde/Hasta son fechas de Costa Rica, ambas inclusive.
    /// </summary>
    Task<PaginaResultado<RegistroAuditoriaDto>> BuscarAsync(FiltroAuditoria filtro, int pagina, int tamanoPagina, CancellationToken ct = default);
}

/// <summary>
/// Consulta de la bitacora de auditoria (solo el Administrador). Los registros los escribe la capa
/// de datos al guardar cualquier cambio; aqui solo se leen.
/// </summary>
public class BuscarAuditoriaCasoUso(IRepositorioAuditoria repositorio)
{
    public const int TamanoPagina = 50;

    /// <summary>Entidades que aparecen en la bitacora (para el filtro de la pantalla).</summary>
    public static readonly IReadOnlyList<string> Entidades =
    [
        "Abonado", "Propiedad", "Servicio", "Medidor", "Medicion", "SeguimientoLectura", "Periodo",
        "CategoriaServicio", "Tarifa", "Recibo", "Organizacion", "Usuario",
    ];

    public async Task<Resultado<PaginaResultado<RegistroAuditoriaDto>>> EjecutarAsync(
        FiltroAuditoria filtro, int pagina, CancellationToken ct = default)
    {
        if (filtro.Desde is DateOnly d && filtro.Hasta is DateOnly h && d > h)
            return Resultado<PaginaResultado<RegistroAuditoriaDto>>.Falla("La fecha inicial es posterior a la final.");
        if (filtro.Entidad is not null && !Entidades.Contains(filtro.Entidad))
            return Resultado<PaginaResultado<RegistroAuditoriaDto>>.Falla("Tipo de registro no reconocido.");

        var limpio = filtro with { Usuario = string.IsNullOrWhiteSpace(filtro.Usuario) ? null : filtro.Usuario.Trim() };
        var resultado = await repositorio.BuscarAsync(limpio, Math.Max(1, pagina), TamanoPagina, ct);
        return Resultado<PaginaResultado<RegistroAuditoriaDto>>.Ok(resultado);
    }
}
