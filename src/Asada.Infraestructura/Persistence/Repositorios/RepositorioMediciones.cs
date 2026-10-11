using Asada.Aplicacion.Common;
using Asada.Aplicacion.Mediciones;
using Asada.Aplicacion.Mediciones.Dtos;
using Asada.Dominio.Entidades;
using Asada.Dominio.Enums;
using Asada.Dominio.Reglas;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Asada.Infraestructura.Persistence.Repositorios;

public class RepositorioMediciones(AsadaDbContext db, IProveedorOrganizacion proveedorOrganizacion) : IRepositorioMediciones
{
    public Task<Servicio?> ObtenerServicioAsync(int servicioId, CancellationToken ct = default)
        => db.Servicios
            .Include(s => s.Propiedad)
                .ThenInclude(p => p!.Abonado)
            .FirstOrDefaultAsync(s => s.Id == servicioId, ct);

    public async Task<IReadOnlyList<Medidor>> ListarMedidoresDeServicioAsync(int servicioId, CancellationToken ct = default)
        => await db.Medidores
            .Where(m => m.ServicioId == servicioId)
            .OrderBy(m => m.FechaInstalacion).ThenBy(m => m.Id)
            .ToListAsync(ct);

    public Task<Medidor?> ObtenerMedidorAsync(int medidorId, CancellationToken ct = default)
        => db.Medidores.FirstOrDefaultAsync(m => m.Id == medidorId, ct);

    public Task<bool> ExisteNumeroSerieAsync(string numeroSerie, CancellationToken ct = default)
        => db.Medidores.AnyAsync(m => m.NumeroSerie == numeroSerie, ct);

    public async Task AgregarMedidorAsync(Medidor medidor, CancellationToken ct = default)
    {
        db.Medidores.Add(medidor);
        await db.SaveChangesAsync(ct);
    }

    public async Task<Periodo> ObtenerOCrearPeriodoAsync(int anio, int mes, CancellationToken ct = default)
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

    public async Task<LecturaAnterior?> ObtenerUltimaLecturaAsync(int servicioId, CancellationToken ct = default)
    {
        var fila = await (
            from m in db.Mediciones
            join p in db.Periodos on m.PeriodoId equals p.Id
            where m.ServicioId == servicioId
            orderby p.Anio descending, p.Mes descending
            select new { m.Lectura, m.MedidorId, p.Anio, p.Mes, m.FechaLectura })
            .FirstOrDefaultAsync(ct);

        return fila is null
            ? null
            : new LecturaAnterior(fila.Lectura, fila.MedidorId, fila.Anio, fila.Mes, fila.FechaLectura);
    }

    public async Task<IReadOnlyList<decimal>> ObtenerConsumosRecientesAsync(int servicioId, int cantidad, CancellationToken ct = default)
        => await (
            from m in db.Mediciones
            join p in db.Periodos on m.PeriodoId equals p.Id
            where m.ServicioId == servicioId
            orderby p.Anio descending, p.Mes descending
            select m.Consumo)
            .Take(cantidad)
            .ToListAsync(ct);

    public Task<bool> ExisteLecturaAsync(int servicioId, int anio, int mes, CancellationToken ct = default)
        => (from m in db.Mediciones
            join p in db.Periodos on m.PeriodoId equals p.Id
            where m.ServicioId == servicioId && p.Anio == anio && p.Mes == mes
            select m.Id).AnyAsync(ct);

    public async Task AgregarMedicionAsync(Medicion medicion, CancellationToken ct = default)
    {
        db.Mediciones.Add(medicion);
        await db.SaveChangesAsync(ct);
    }

    public Task<SeguimientoLectura?> ObtenerSeguimientoAsync(int servicioId, int periodoId, CancellationToken ct = default)
        => db.SeguimientosLectura.FirstOrDefaultAsync(s => s.ServicioId == servicioId && s.PeriodoId == periodoId, ct);

    public async Task AgregarSeguimientoAsync(SeguimientoLectura seguimiento, CancellationToken ct = default)
    {
        db.SeguimientosLectura.Add(seguimiento);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<FilaLecturaPeriodoDto>> ListarFilasDelPeriodoAsync(int anio, int mes, CancellationToken ct = default)
    {
        var finVentana = ReglasPeriodo.FinVentana(anio, mes);

        // Servicios activos (de propiedades y abonados activos) que ya existian en ese periodo.
        // Los filtros globales de organizacion se aplican solos a cada tabla consultada.
        var servicios = await (
            from s in db.Servicios
            join p in db.Propiedades on s.PropiedadId equals p.Id
            join a in db.Abonados on p.AbonadoId equals a.Id
            where s.Estado == EstadoRegistro.Activo
                  && p.Estado == EstadoRegistro.Activo
                  && a.Estado == EstadoRegistro.Activo
                  && s.FechaInicio <= finVentana
            orderby a.Nombre, s.Id
            select new { ServicioId = s.Id, AbonadoId = a.Id, a.Codigo, a.Nombre, p.Direccion })
            .ToListAsync(ct);

        var ids = servicios.Select(x => x.ServicioId).ToList();

        var medidoresActivos = await db.Medidores
            .Where(m => ids.Contains(m.ServicioId) && m.Estado == EstadoMedidor.Activo)
            .Select(m => new { m.ServicioId, m.NumeroSerie })
            .ToListAsync(ct);

        var lecturas = await (
            from m in db.Mediciones
            join p in db.Periodos on m.PeriodoId equals p.Id
            where ids.Contains(m.ServicioId) && p.Anio == anio && p.Mes == mes
            select new { m.ServicioId, m.FechaLectura, m.Lectura, m.Consumo, m.RequiereRevision, m.MotivosRevision, m.Observaciones })
            .ToListAsync(ct);

        var seguimientos = await (
            from z in db.SeguimientosLectura
            join p in db.Periodos on z.PeriodoId equals p.Id
            where ids.Contains(z.ServicioId) && p.Anio == anio && p.Mes == mes
            select new { z.ServicioId, z.Decision, z.Nota })
            .ToListAsync(ct);

        return servicios.Select(s =>
        {
            var medidor = medidoresActivos.FirstOrDefault(m => m.ServicioId == s.ServicioId);
            var lectura = lecturas.FirstOrDefault(l => l.ServicioId == s.ServicioId);
            var seguimiento = seguimientos.FirstOrDefault(z => z.ServicioId == s.ServicioId);

            return new FilaLecturaPeriodoDto(
                s.ServicioId, s.AbonadoId, s.Codigo, s.Nombre, s.Direccion,
                medidor?.NumeroSerie,
                lectura?.FechaLectura, lectura?.Lectura, lectura?.Consumo,
                lectura?.RequiereRevision ?? false, lectura?.MotivosRevision, lectura?.Observaciones,
                seguimiento?.Decision, seguimiento?.Nota);
        }).ToList();
    }

    public Task GuardarCambiosAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
