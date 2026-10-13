using Asada.Aplicacion.Configuracion;
using Asada.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Asada.Infraestructura.Persistence.Repositorios;

public class RepositorioConfiguracion(AsadaDbContext db) : IRepositorioConfiguracion
{
    public Task<Organizacion?> ObtenerOrganizacionAsync(int organizacionId, CancellationToken ct = default)
        => db.Organizaciones.FirstOrDefaultAsync(o => o.Id == organizacionId, ct);

    public Task GuardarCambiosAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
