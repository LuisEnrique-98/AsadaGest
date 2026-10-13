using Asada.Aplicacion.Auditoria;
using Asada.Aplicacion.Common;
using Microsoft.EntityFrameworkCore;

namespace Asada.Infraestructura.Persistence.Repositorios;

public class RepositorioAuditoria(AsadaDbContext db) : IRepositorioAuditoria
{
    // Las fechas del filtro son dias de Costa Rica (UTC-6); la bitacora guarda UTC.
    private static readonly TimeSpan DesfaseCostaRica = TimeSpan.FromHours(6);

    public async Task<PaginaResultado<RegistroAuditoriaDto>> BuscarAsync(
        FiltroAuditoria filtro, int pagina, int tamanoPagina, CancellationToken ct = default)
    {
        var consulta = db.Auditoria.AsNoTracking();

        if (filtro.Desde is DateOnly desde)
        {
            var inicio = DateTime.SpecifyKind(desde.ToDateTime(TimeOnly.MinValue) + DesfaseCostaRica, DateTimeKind.Utc);
            consulta = consulta.Where(a => a.Fecha >= inicio);
        }
        if (filtro.Hasta is DateOnly hasta)
        {
            var fin = DateTime.SpecifyKind(hasta.AddDays(1).ToDateTime(TimeOnly.MinValue) + DesfaseCostaRica, DateTimeKind.Utc);
            consulta = consulta.Where(a => a.Fecha < fin);
        }
        if (filtro.Entidad is string entidad) consulta = consulta.Where(a => a.Entidad == entidad);
        if (filtro.EntidadId is long id) consulta = consulta.Where(a => a.EntidadId == id);
        if (filtro.Usuario is string usuario)
        {
            var patron = $"%{usuario.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_")}%";
            consulta = consulta.Where(a => EF.Functions.ILike(a.UsuarioNombre, patron));
        }

        var total = await consulta.CountAsync(ct);
        var filas = await consulta
            .OrderByDescending(a => a.Fecha).ThenByDescending(a => a.Id)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToListAsync(ct);

        var items = filas.Select(a => new RegistroAuditoriaDto(
            a.Id, a.Fecha, a.UsuarioNombre, a.Accion, a.Entidad, a.EntidadId,
            CambioAuditoria.Deserializar(a.Cambios))).ToList();

        return new PaginaResultado<RegistroAuditoriaDto>(items, total, pagina, tamanoPagina);
    }
}
