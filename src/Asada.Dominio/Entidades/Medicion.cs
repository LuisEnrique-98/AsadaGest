using Asada.Dominio.Enums;
using Asada.Dominio.Reglas;

namespace Asada.Dominio.Entidades;

/// <summary>
/// Lectura de un servicio en un periodo, junto con el consumo ya calculado.
/// El consumo se guarda en el momento del registro (no se recalcula despues) para que un
/// recibo emitido sea reproducible aunque cambie el historial de medidores.
///
/// Sprint 5: una lectura se puede corregir o anular (solo el Administrador, con motivo), siempre
/// que sea la ultima del servicio, no tenga recibo emitido y su periodo este abierto. Nunca se
/// borra: la anulada queda guardada con su motivo y deja de contar para todo.
/// </summary>
public class Medicion
{
    public int Id { get; set; }
    public int OrganizacionId { get; set; }
    public int ServicioId { get; set; }
    public int MedidorId { get; set; }
    public int PeriodoId { get; set; }

    /// <summary>Fecha real en que el lector tomo la lectura.</summary>
    public DateOnly FechaLectura { get; set; }

    public decimal Lectura { get; set; }
    public decimal Consumo { get; set; }

    /// <summary>True si un operador debe revisarla (lectura tardia, periodo anterior sin lectura, consumo anormal).</summary>
    public bool RequiereRevision { get; set; }

    /// <summary>Motivos de la revision, separados por " | ".</summary>
    public string? MotivosRevision { get; set; }

    public string? Observaciones { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public EstadoMedicion Estado { get; private set; } = EstadoMedicion.Vigente;

    /// <summary>Usuario que registro la lectura en el sistema (null en lecturas anteriores al Sprint 5).</summary>
    public int? RegistradaPorUsuarioId { get; set; }
    public string? RegistradaPorNombre { get; set; }

    // Ultima correccion (el historial completo de correcciones esta en la auditoria).
    public DateTime? FechaUltimaCorreccion { get; private set; }
    public int? CorregidaPorUsuarioId { get; private set; }
    public string? CorregidaPorNombre { get; private set; }
    public string? MotivoCorreccion { get; private set; }

    // Anulacion.
    public DateTime? FechaAnulacion { get; private set; }
    public int? AnuladaPorUsuarioId { get; private set; }
    public string? AnuladaPorNombre { get; private set; }
    public string? MotivoAnulacion { get; private set; }

    public bool EstaVigente => Estado == EstadoMedicion.Vigente;

    /// <summary>
    /// Reemplaza la fecha, la lectura y el consumo (ya recalculado por quien llama) y deja
    /// constancia de quien corrigio y por que.
    /// </summary>
    public void Corregir(
        DateOnly fechaLectura, decimal lectura, decimal consumo, IReadOnlyList<string> motivosRevision,
        string? observaciones, string motivo, int? usuarioId, string usuarioNombre, DateTime ahoraUtc)
    {
        if (!EstaVigente) throw new InvalidOperationException("No se puede corregir una lectura anulada.");
        if (ReglasMotivo.Validar(motivo) is string error) throw new InvalidOperationException(error);

        FechaLectura = fechaLectura;
        Lectura = lectura;
        Consumo = consumo;
        RequiereRevision = motivosRevision.Count > 0;
        MotivosRevision = motivosRevision.Count > 0 ? string.Join(" | ", motivosRevision) : null;
        Observaciones = observaciones;

        FechaUltimaCorreccion = ahoraUtc;
        CorregidaPorUsuarioId = usuarioId;
        CorregidaPorNombre = usuarioNombre;
        MotivoCorreccion = motivo.Trim();
    }

    public void Anular(string motivo, int? usuarioId, string usuarioNombre, DateTime ahoraUtc)
    {
        if (!EstaVigente) throw new InvalidOperationException("La lectura ya esta anulada.");
        if (ReglasMotivo.Validar(motivo) is string error) throw new InvalidOperationException(error);

        Estado = EstadoMedicion.Anulada;
        FechaAnulacion = ahoraUtc;
        AnuladaPorUsuarioId = usuarioId;
        AnuladaPorNombre = usuarioNombre;
        MotivoAnulacion = motivo.Trim();
    }
}
