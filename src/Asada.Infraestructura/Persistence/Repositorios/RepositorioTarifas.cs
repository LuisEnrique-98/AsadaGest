using Asada.Aplicacion.Tarifas;
using Asada.Dominio.Entidades;
using Asada.Dominio.Enums;
using Microsoft.EntityFrameworkCore;

namespace Asada.Infraestructura.Persistence.Repositorios;

public class RepositorioTarifas(AsadaDbContext db) : IRepositorioTarifas
{
    public async Task<IReadOnlyList<CategoriaServicio>> ListarCategoriasAsync(CancellationToken ct = default)
        => await db.CategoriasServicio.OrderBy(c => c.Nombre).ToListAsync(ct);

    public async Task<IReadOnlyDictionary<int, int>> ContarServiciosPorCategoriaAsync(CancellationToken ct = default)
        => await db.Servicios
            .Where(s => s.CategoriaId != null && s.Estado == EstadoRegistro.Activo)
            .GroupBy(s => s.CategoriaId!.Value)
            .Select(g => new { CategoriaId = g.Key, Cantidad = g.Count() })
            .ToDictionaryAsync(x => x.CategoriaId, x => x.Cantidad, ct);

    public Task<int> ContarServiciosActivosSinCategoriaAsync(CancellationToken ct = default)
        => db.Servicios.CountAsync(s => s.CategoriaId == null && s.Estado == EstadoRegistro.Activo, ct);

    public Task<CategoriaServicio?> ObtenerCategoriaAsync(int categoriaId, CancellationToken ct = default)
        => db.CategoriasServicio.FirstOrDefaultAsync(c => c.Id == categoriaId, ct);

    public Task<bool> ExisteCategoriaConNombreAsync(string nombre, CancellationToken ct = default)
    {
        var buscado = nombre.ToLower();
        return db.CategoriasServicio.AnyAsync(c => c.Nombre.ToLower() == buscado, ct);
    }

    public async Task AgregarCategoriaAsync(CategoriaServicio categoria, CancellationToken ct = default)
    {
        db.CategoriasServicio.Add(categoria);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<Tarifa>> ListarTarifasAsync(CancellationToken ct = default)
        => await db.Tarifas.AsNoTracking().Include(t => t.Bloques).ToListAsync(ct);

    public Task<Tarifa?> ObtenerTarifaAsync(int tarifaId, CancellationToken ct = default)
        => db.Tarifas.AsNoTracking()
            .Include(t => t.Bloques)
            .Include(t => t.Categoria)
            .FirstOrDefaultAsync(t => t.Id == tarifaId, ct);

    public async Task AgregarTarifaAsync(Tarifa tarifa, CancellationToken ct = default)
    {
        db.Tarifas.Add(tarifa);
        await db.SaveChangesAsync(ct);
    }

    public async Task<(int Anio, int Mes)?> ObtenerUltimoPeriodoFacturadoAsync(int categoriaId, CancellationToken ct = default)
    {
        var ultimo = await (
            from r in db.Recibos
            join t in db.Tarifas on r.TarifaId equals t.Id
            join p in db.Periodos on r.PeriodoId equals p.Id
            where t.CategoriaId == categoriaId && r.Estado == EstadoRecibo.Emitido
            orderby p.Anio descending, p.Mes descending
            select new { p.Anio, p.Mes })
            .FirstOrDefaultAsync(ct);

        return ultimo is null ? null : (ultimo.Anio, ultimo.Mes);
    }

    public Task<Servicio?> ObtenerServicioAsync(int servicioId, CancellationToken ct = default)
        => db.Servicios
            .Include(s => s.Propiedad)
                .ThenInclude(p => p!.Abonado)
            .FirstOrDefaultAsync(s => s.Id == servicioId, ct);

    public Task GuardarCambiosAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
