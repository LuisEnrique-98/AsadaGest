using Asada.Aplicacion.Organizaciones;
using Asada.Dominio.Entidades;
using Asada.Dominio.Enums;
using Microsoft.EntityFrameworkCore;

namespace Asada.Infraestructura.Persistence.Repositorios;

/// <summary>
/// ASADAs de la plataforma (Sprint 5b). Las consultas pasan por el filtro global de organizacion:
/// el Superadministrador ve todas; cualquier otro usuario, solo la suya.
/// </summary>
public class RepositorioOrganizaciones(AsadaDbContext db) : IRepositorioOrganizaciones
{
    public async Task<IReadOnlyList<OrganizacionResumenDto>> ListarAsync(CancellationToken ct = default)
    {
        var filas = await db.Organizaciones
            .OrderBy(o => o.Nombre)
            .Select(o => new
            {
                o.Id, o.Codigo, o.Nombre, o.Estado, o.FechaCreacion,
                Usuarios = db.Users.Count(u => u.OrganizacionId == o.Id && u.Estado == EstadoUsuario.Activo),
            })
            .ToListAsync(ct);
        return filas.Select(f => new OrganizacionResumenDto(f.Id, f.Codigo, f.Nombre, f.Estado, f.Usuarios, f.FechaCreacion)).ToList();
    }

    public Task<Organizacion?> ObtenerAsync(int organizacionId, CancellationToken ct = default)
        => db.Organizaciones.FirstOrDefaultAsync(o => o.Id == organizacionId, ct);

    public Task<bool> ExisteCodigoAsync(string codigo, CancellationToken ct = default)
        => db.Organizaciones.IgnoreQueryFilters().AnyAsync(o => o.Codigo == codigo, ct);

    public async Task AgregarAsync(Organizacion organizacion, CancellationToken ct = default)
        => await db.Organizaciones.AddAsync(organizacion, ct);

    public Task GuardarCambiosAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
