using Asada.Aplicacion.Common;
using Asada.Aplicacion.Periodos;
using Asada.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Asada.Infraestructura.Persistence.Repositorios;

public class RepositorioPeriodos(AsadaDbContext db, IProveedorOrganizacion proveedorOrganizacion) : IRepositorioPeriodos
{
    public Task<Periodo?> ObtenerPeriodoAsync(int anio, int mes, CancellationToken ct = default)
        => db.Periodos.FirstOrDefaultAsync(p => p.Anio == anio && p.Mes == mes, ct);

    public Task<Periodo> ObtenerOCrearPeriodoAsync(int anio, int mes, CancellationToken ct = default)
        => ObtenerOCrearAsync(db, proveedorOrganizacion, anio, mes, ct);

    public Task GuardarCambiosAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);

    /// <summary>Compartido con RepositorioMediciones: devuelve el periodo, creandolo si no existe.</summary>
    internal static async Task<Periodo> ObtenerOCrearAsync(
        AsadaDbContext db, IProveedorOrganizacion proveedorOrganizacion, int anio, int mes, CancellationToken ct)
    {
        var periodo = await db.Periodos.FirstOrDefaultAsync(p => p.Anio == anio && p.Mes == mes, ct);
        if (periodo is not null) return periodo;

        var organizacionId = proveedorOrganizacion.OrganizacionId
            ?? throw new InvalidOperationException("Solo un usuario de una ASADA puede crear periodos.");
        try
        {
            periodo = new Periodo { OrganizacionId = organizacionId, Anio = anio, Mes = mes };
            db.Periodos.Add(periodo);
            await db.SaveChangesAsync(ct);
            return periodo;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Otro usuario creo el mismo periodo al mismo tiempo: se descarta el nuestro y se usa el suyo.
            db.Entry(periodo!).State = EntityState.Detached;
            return await db.Periodos.FirstAsync(p => p.Anio == anio && p.Mes == mes, ct);
        }
    }
}
