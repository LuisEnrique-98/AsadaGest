using Asada.Aplicacion.Recibos;
using Asada.Dominio.Entidades;
using Asada.Dominio.Enums;
using Asada.Dominio.Reglas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace Asada.Infraestructura.Persistence.Repositorios;

public class RepositorioRecibos(AsadaDbContext db) : IRepositorioRecibos
{
    public async Task<IReadOnlyList<ServicioAFacturar>> ListarServiciosDelPeriodoAsync(int anio, int mes, CancellationToken ct = default)
    {
        var finMes = ReglasTarifa.FechaReferencia(anio, mes);
        var periodo = await db.Periodos.AsNoTracking().FirstOrDefaultAsync(p => p.Anio == anio && p.Mes == mes, ct);
        var periodoId = periodo?.Id ?? 0;

        // Servicios activos que ya existian al cerrar el mes, mas los que ya tienen recibo en el
        // periodo aunque despues se hayan desactivado (para que el listado no "pierda" recibos).
        // Los filtros globales de organizacion se aplican solos a cada tabla consultada.
        var servicios = await (
            from s in db.Servicios
            join p in db.Propiedades on s.PropiedadId equals p.Id
            join a in db.Abonados on p.AbonadoId equals a.Id
            where (s.Estado == EstadoRegistro.Activo
                   && p.Estado == EstadoRegistro.Activo
                   && a.Estado == EstadoRegistro.Activo
                   && s.FechaInicio <= finMes)
                  || db.Recibos.Any(r => r.ServicioId == s.Id && r.PeriodoId == periodoId && r.Estado == EstadoRecibo.Emitido)
            orderby a.Nombre, s.Id
            select new { ServicioId = s.Id, AbonadoId = a.Id, a.Codigo, a.Nombre, p.Direccion, s.CategoriaId })
            .ToListAsync(ct);

        var ids = servicios.Select(x => x.ServicioId).ToList();

        var categorias = await db.CategoriasServicio.AsNoTracking()
            .ToDictionaryAsync(c => c.Id, c => c.Nombre, ct);

        var lecturas = await db.Mediciones.AsNoTracking()
            .Where(m => m.PeriodoId == periodoId && ids.Contains(m.ServicioId))
            .Select(m => new { m.ServicioId, m.Id, m.Lectura, m.FechaLectura, m.Consumo, m.RequiereRevision, m.MotivosRevision, m.MedidorId })
            .ToListAsync(ct);

        var idsMedidores = lecturas.Select(l => l.MedidorId).Distinct().ToList();
        var series = await db.Medidores.AsNoTracking()
            .Where(m => idsMedidores.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, m => m.NumeroSerie, ct);

        // Lectura anterior de cada servicio: la ultima de un periodo previo. Se resuelve en
        // memoria (pocas columnas; unos miles de filas por anio en una ASADA).
        var previas = await (
            from m in db.Mediciones.AsNoTracking()
            join p in db.Periodos on m.PeriodoId equals p.Id
            where ids.Contains(m.ServicioId) && (p.Anio < anio || (p.Anio == anio && p.Mes < mes))
            select new { m.ServicioId, m.Lectura, m.MedidorId, p.Anio, p.Mes })
            .ToListAsync(ct);
        var anterior = previas
            .GroupBy(x => x.ServicioId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Anio).ThenByDescending(x => x.Mes).First());

        var recibos = await db.Recibos.AsNoTracking()
            .Where(r => r.PeriodoId == periodoId && r.Estado == EstadoRecibo.Emitido)
            .Select(r => new { r.ServicioId, r.Id, r.Numero, r.Total })
            .ToListAsync(ct);

        return servicios.Select(s =>
        {
            var l = lecturas.FirstOrDefault(x => x.ServicioId == s.ServicioId);
            var previa = anterior.GetValueOrDefault(s.ServicioId);
            var recibo = recibos.FirstOrDefault(x => x.ServicioId == s.ServicioId);

            return new ServicioAFacturar(
                s.ServicioId, s.AbonadoId, s.Codigo, s.Nombre, s.Direccion,
                s.CategoriaId, s.CategoriaId is int c ? categorias.GetValueOrDefault(c) : null,
                l?.Id, l?.Lectura, l?.FechaLectura, l?.Consumo,
                l?.RequiereRevision ?? false, l?.MotivosRevision,
                l is null ? null : series.GetValueOrDefault(l.MedidorId), l?.MedidorId,
                previa?.Lectura, previa?.MedidorId,
                recibo?.Id, recibo?.Numero, recibo?.Total);
        }).ToList();
    }

    public async Task<IReadOnlyList<Tarifa>> ListarTarifasAsync(CancellationToken ct = default)
        => await db.Tarifas.AsNoTracking().Include(t => t.Bloques).ToListAsync(ct);

    public Task<Periodo?> ObtenerPeriodoAsync(int anio, int mes, CancellationToken ct = default)
        => db.Periodos.AsNoTracking().FirstOrDefaultAsync(p => p.Anio == anio && p.Mes == mes, ct);

    public async Task<bool> AgregarRecibosAsync(int anio, IReadOnlyList<Recibo> recibos, CancellationToken ct = default)
    {
        if (recibos.Count == 0) return true;
        var organizacionId = recibos[0].OrganizacionId;
        if (recibos.Any(r => r.OrganizacionId != organizacionId))
            throw new InvalidOperationException("Todos los recibos de una generacion deben ser de la misma organizacion.");

        await using var transaccion = await db.Database.BeginTransactionAsync(ct);

        // Reserva atomica de un bloque de correlativos para este anio: el INSERT ... ON CONFLICT
        // ... RETURNING toma un bloqueo de fila, asi que dos generaciones simultaneas nunca
        // reciben numeros repetidos. Si algo falla mas abajo, la transaccion se revierte y los
        // numeros no se "queman". Es SQL directo (no pasa por el filtro global): filtra por id.
        var conexion = db.Database.GetDbConnection();
        int ultimo;
        await using (var comando = conexion.CreateCommand())
        {
            comando.Transaction = transaccion.GetDbTransaction();
            comando.CommandText =
                "INSERT INTO correlativos_recibo (organizacion_id, anio, ultimo) VALUES (@org, @anio, @cantidad) " +
                "ON CONFLICT (organizacion_id, anio) DO UPDATE SET ultimo = correlativos_recibo.ultimo + EXCLUDED.ultimo " +
                "RETURNING ultimo";
            comando.Parameters.Add(new NpgsqlParameter("org", organizacionId));
            comando.Parameters.Add(new NpgsqlParameter("anio", anio));
            comando.Parameters.Add(new NpgsqlParameter("cantidad", recibos.Count));
            ultimo = Convert.ToInt32(await comando.ExecuteScalarAsync(ct));
        }

        string codigoOrganizacion;
        await using (var comando = conexion.CreateCommand())
        {
            comando.Transaction = transaccion.GetDbTransaction();
            comando.CommandText = "SELECT codigo FROM organizaciones WHERE id = @org";
            comando.Parameters.Add(new NpgsqlParameter("org", organizacionId));
            codigoOrganizacion = (string?)await comando.ExecuteScalarAsync(ct)
                ?? throw new InvalidOperationException($"La organizacion {organizacionId} no existe.");
        }

        var correlativo = ultimo - recibos.Count;
        foreach (var recibo in recibos)
        {
            correlativo++;
            recibo.Correlativo = correlativo;
            recibo.Numero = ReglasRecibo.GenerarNumero(codigoOrganizacion, anio, correlativo);
        }

        db.Recibos.AddRange(recibos);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Otro usuario facturo alguno de estos servicios al mismo tiempo (indice unico
            // servicio + periodo). Se descarta todo: la transaccion se revierte al salir.
            foreach (var recibo in recibos) db.Entry(recibo).State = EntityState.Detached;
            return false;
        }

        await transaccion.CommitAsync(ct);
        return true;
    }

    public async Task<(Recibo Recibo, Periodo Periodo)?> ObtenerReciboAsync(int reciboId, CancellationToken ct = default)
    {
        var recibo = await db.Recibos.AsNoTracking()
            .Include(r => r.Detalles)
            .FirstOrDefaultAsync(r => r.Id == reciboId, ct);
        if (recibo is null) return null;

        var periodo = await db.Periodos.AsNoTracking().FirstAsync(p => p.Id == recibo.PeriodoId, ct);
        return (recibo, periodo);
    }

    public Task<Organizacion?> ObtenerOrganizacionAsync(int organizacionId, CancellationToken ct = default)
        => db.Organizaciones.AsNoTracking().FirstOrDefaultAsync(o => o.Id == organizacionId, ct);
}
