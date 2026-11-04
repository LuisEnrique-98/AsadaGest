using Asada.Aplicacion.Common;
using Asada.Aplicacion.Mediciones;
using Asada.Aplicacion.Mediciones.Dtos;
using Asada.Dominio.Entidades;
using Asada.Dominio.Enums;
using Asada.Dominio.Reglas;

namespace Asada.Aplicacion.Tests;

public sealed class ProveedorFechaFalso(DateOnly hoy) : IProveedorFecha
{
    public DateOnly Hoy => hoy;
}

public sealed class ProveedorUsuarioFalso(int? usuarioId = 7, string nombre = "Ana Operadora") : IProveedorUsuario
{
    public int? UsuarioId => usuarioId;
    public string Nombre => nombre;
}

/// <summary>Repositorio en memoria que imita las reglas de orden y busqueda de RepositorioMediciones (EF Core).</summary>
public sealed class RepositorioMedicionesEnMemoria : IRepositorioMediciones
{
    public List<Servicio> Servicios { get; } = [];
    public List<Medidor> Medidores { get; } = [];
    public List<Periodo> Periodos { get; } = [];
    public List<Medicion> Mediciones { get; } = [];
    public List<SeguimientoLectura> Seguimientos { get; } = [];
    public int Guardados { get; private set; }

    /// <summary>Umbral de consumo anormal de la ASADA (Organizacion.PorcentajeConsumoAnormal).</summary>
    public decimal PorcentajeConsumoAnormal { get; set; } = 50m;

    /// <summary>Recibos emitidos por id de lectura (para las reglas de correccion y anulacion).</summary>
    public Dictionary<int, ReciboDeLectura> RecibosEmitidos { get; } = [];

    /// <summary>Crea un abonado con una propiedad y un servicio (todos con Id=1) y lo registra.</summary>
    public Servicio NuevoServicio(int organizacionId = 1, EstadoRegistro estado = EstadoRegistro.Activo)
    {
        var abonado = new Abonado
        {
            Id = 1, OrganizacionId = organizacionId, Codigo = "CB-AB-000001",
            Identificacion = "102340567", Nombre = "Maria Rojas",
        };
        var propiedad = abonado.AgregarPropiedad("Casa azul");
        propiedad.Id = 1;
        var servicio = propiedad.AgregarServicio(new DateOnly(2025, 1, 1));
        servicio.Id = Servicios.Count + 1;
        servicio.Estado = estado;
        Servicios.Add(servicio);
        return servicio;
    }

    public Task<Servicio?> ObtenerServicioAsync(int servicioId, CancellationToken ct = default)
        => Task.FromResult(Servicios.FirstOrDefault(s => s.Id == servicioId));

    public Task<IReadOnlyList<Medidor>> ListarMedidoresDeServicioAsync(int servicioId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Medidor>>(
            Medidores.Where(m => m.ServicioId == servicioId).OrderBy(m => m.FechaInstalacion).ThenBy(m => m.Id).ToList());

    public Task<Medidor?> ObtenerMedidorAsync(int medidorId, CancellationToken ct = default)
        => Task.FromResult(Medidores.FirstOrDefault(m => m.Id == medidorId));

    public Task<bool> ExisteNumeroSerieAsync(string numeroSerie, CancellationToken ct = default)
        => Task.FromResult(Medidores.Any(m => m.NumeroSerie == numeroSerie));

    public Task AgregarMedidorAsync(Medidor medidor, CancellationToken ct = default)
    {
        medidor.Id = Medidores.Count + 1;
        Medidores.Add(medidor);
        return Task.CompletedTask;
    }

    public Task<Periodo> ObtenerOCrearPeriodoAsync(int anio, int mes, CancellationToken ct = default)
    {
        var periodo = Periodos.FirstOrDefault(p => p.Anio == anio && p.Mes == mes);
        if (periodo is null)
        {
            periodo = new Periodo { Id = Periodos.Count + 1, OrganizacionId = 1, Anio = anio, Mes = mes };
            Periodos.Add(periodo);
        }
        return Task.FromResult(periodo);
    }

    // Como el filtro global de EF Core: las lecturas anuladas no existen para las consultas.
    private IEnumerable<(Medicion M, Periodo P)> LecturasDe(int servicioId)
        => Mediciones.Where(m => m.ServicioId == servicioId && m.EstaVigente)
            .Join(Periodos, m => m.PeriodoId, p => p.Id, (m, p) => (m, p))
            .OrderByDescending(x => x.p.Anio).ThenByDescending(x => x.p.Mes);

    public Task<LecturaAnterior?> ObtenerUltimaLecturaAsync(int servicioId, CancellationToken ct = default)
    {
        var x = LecturasDe(servicioId).FirstOrDefault();
        return Task.FromResult<LecturaAnterior?>(
            x.M is null ? null : new LecturaAnterior(x.M.Lectura, x.M.MedidorId, x.P.Anio, x.P.Mes, x.M.FechaLectura));
    }

    public Task<Periodo?> ObtenerPeriodoAsync(int anio, int mes, CancellationToken ct = default)
        => Task.FromResult(Periodos.FirstOrDefault(p => p.Anio == anio && p.Mes == mes));

    public Task<Periodo?> ObtenerPeriodoPorIdAsync(int periodoId, CancellationToken ct = default)
        => Task.FromResult(Periodos.FirstOrDefault(p => p.Id == periodoId));

    public Task<decimal> ObtenerPorcentajeConsumoAnormalAsync(CancellationToken ct = default)
        => Task.FromResult(PorcentajeConsumoAnormal);

    private IEnumerable<(Medicion M, Periodo P)> LecturasAnterioresA(int servicioId, int anio, int mes)
        => LecturasDe(servicioId).Where(x => ReglasPeriodo.Comparar(x.P.Anio, x.P.Mes, anio, mes) < 0);

    public Task<LecturaAnterior?> ObtenerLecturaAnteriorAAsync(int servicioId, int anio, int mes, CancellationToken ct = default)
    {
        var x = LecturasAnterioresA(servicioId, anio, mes).FirstOrDefault();
        return Task.FromResult<LecturaAnterior?>(
            x.M is null ? null : new LecturaAnterior(x.M.Lectura, x.M.MedidorId, x.P.Anio, x.P.Mes, x.M.FechaLectura));
    }

    public Task<IReadOnlyList<decimal>> ObtenerConsumosAnterioresAAsync(int servicioId, int anio, int mes, int cantidad, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<decimal>>(LecturasAnterioresA(servicioId, anio, mes).Take(cantidad).Select(x => x.M.Consumo).ToList());

    public Task<Medicion?> ObtenerMedicionAsync(int medicionId, CancellationToken ct = default)
        => Task.FromResult(Mediciones.FirstOrDefault(m => m.Id == medicionId && m.EstaVigente));

    public Task<ReciboDeLectura?> ObtenerReciboEmitidoDeLecturaAsync(int medicionId, CancellationToken ct = default)
        => Task.FromResult(RecibosEmitidos.GetValueOrDefault(medicionId));

    public Task<IReadOnlyList<decimal>> ObtenerConsumosRecientesAsync(int servicioId, int cantidad, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<decimal>>(LecturasDe(servicioId).Take(cantidad).Select(x => x.M.Consumo).ToList());

    public Task<bool> ExisteLecturaAsync(int servicioId, int anio, int mes, CancellationToken ct = default)
        => Task.FromResult(LecturasDe(servicioId).Any(x => x.P.Anio == anio && x.P.Mes == mes));

    public Task AgregarMedicionAsync(Medicion medicion, CancellationToken ct = default)
    {
        medicion.Id = Mediciones.Count + 1;
        Mediciones.Add(medicion);
        return Task.CompletedTask;
    }

    public Task<SeguimientoLectura?> ObtenerSeguimientoAsync(int servicioId, int periodoId, CancellationToken ct = default)
        => Task.FromResult(Seguimientos.FirstOrDefault(s => s.ServicioId == servicioId && s.PeriodoId == periodoId));

    public Task AgregarSeguimientoAsync(SeguimientoLectura seguimiento, CancellationToken ct = default)
    {
        seguimiento.Id = Seguimientos.Count + 1;
        Seguimientos.Add(seguimiento);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<FilaLecturaPeriodoDto>> ListarFilasDelPeriodoAsync(int anio, int mes, CancellationToken ct = default)
    {
        var filas = Servicios.Where(s => s.Estado == EstadoRegistro.Activo).Select(s =>
        {
            var lectura = LecturasDe(s.Id).FirstOrDefault(x => x.P.Anio == anio && x.P.Mes == mes);
            var periodo = Periodos.FirstOrDefault(p => p.Anio == anio && p.Mes == mes);
            var seg = periodo is null ? null : Seguimientos.FirstOrDefault(z => z.ServicioId == s.Id && z.PeriodoId == periodo.Id);
            var activo = Medidores.FirstOrDefault(m => m.ServicioId == s.Id && m.Estado == EstadoMedidor.Activo);
            var ab = s.Propiedad!.Abonado!;
            return new FilaLecturaPeriodoDto(
                s.Id, ab.Id, ab.Codigo, ab.Nombre, s.Propiedad.Direccion, activo?.NumeroSerie,
                lectura.M?.FechaLectura, lectura.M?.Lectura, lectura.M?.Consumo,
                lectura.M?.RequiereRevision ?? false, lectura.M?.MotivosRevision, lectura.M?.Observaciones,
                seg?.Decision, seg?.Nota, lectura.M?.Id, lectura.M?.RegistradaPorNombre);
        }).ToList();
        return Task.FromResult<IReadOnlyList<FilaLecturaPeriodoDto>>(filas);
    }

    public Task GuardarCambiosAsync(CancellationToken ct = default)
    {
        Guardados++;
        return Task.CompletedTask;
    }
}
