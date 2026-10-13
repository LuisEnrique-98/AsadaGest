using Asada.Dominio.Enums;
using Asada.Dominio.Reglas;

namespace Asada.Dominio.Entidades;

/// <summary>
/// Mes de facturacion de una organizacion. Se crea la primera vez que se usa.
///
/// Sprint 5: el Administrador cierra el periodo cuando termina de facturarlo. Cerrado, no se
/// registran ni corrigen lecturas ni se generan o anulan recibos de ese mes. Solo el
/// Administrador lo puede reabrir, indicando el motivo.
/// </summary>
public class Periodo
{
    public int Id { get; set; }
    public int OrganizacionId { get; set; }
    public int Anio { get; set; }
    public int Mes { get; set; }
    public EstadoPeriodo Estado { get; private set; } = EstadoPeriodo.Abierto;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public DateOnly InicioVentanaLectura => ReglasPeriodo.InicioVentana(Anio, Mes);
    public DateOnly FinVentanaLectura => ReglasPeriodo.FinVentana(Anio, Mes);

    public bool EstaCerrado => Estado == EstadoPeriodo.Cerrado;

    // Ultimo cierre y ultima reapertura (el historial completo esta en la auditoria).
    public DateTime? FechaCierre { get; private set; }
    public int? CerradoPorUsuarioId { get; private set; }
    public string? CerradoPorNombre { get; private set; }
    public DateTime? FechaReapertura { get; private set; }
    public int? ReabiertoPorUsuarioId { get; private set; }
    public string? ReabiertoPorNombre { get; private set; }
    public string? MotivoReapertura { get; private set; }

    public void Cerrar(int? usuarioId, string usuarioNombre, DateTime ahoraUtc)
    {
        if (EstaCerrado) throw new InvalidOperationException("El periodo ya esta cerrado.");
        Estado = EstadoPeriodo.Cerrado;
        FechaCierre = ahoraUtc;
        CerradoPorUsuarioId = usuarioId;
        CerradoPorNombre = usuarioNombre;
    }

    public void Reabrir(string motivo, int? usuarioId, string usuarioNombre, DateTime ahoraUtc)
    {
        if (!EstaCerrado) throw new InvalidOperationException("El periodo no esta cerrado.");
        if (ReglasMotivo.Validar(motivo) is string error) throw new InvalidOperationException(error);
        Estado = EstadoPeriodo.Abierto;
        FechaReapertura = ahoraUtc;
        ReabiertoPorUsuarioId = usuarioId;
        ReabiertoPorNombre = usuarioNombre;
        MotivoReapertura = motivo.Trim();
    }
}
