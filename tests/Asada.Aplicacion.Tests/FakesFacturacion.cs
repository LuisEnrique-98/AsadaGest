using Asada.Aplicacion.Recibos;
using Asada.Aplicacion.Tarifas;
using Asada.Dominio.Entidades;
using Asada.Dominio.Enums;
using Asada.Dominio.Reglas;

namespace Asada.Aplicacion.Tests;

/// <summary>Repositorio en memoria que reemplaza a RepositorioTarifas (EF Core) en las pruebas.</summary>
public sealed class RepositorioTarifasEnMemoria : IRepositorioTarifas
{
    public List<CategoriaServicio> Categorias { get; } = [];
    public List<Tarifa> Tarifas { get; } = [];
    public List<Servicio> Servicios { get; } = [];
    public (int Anio, int Mes)? UltimoPeriodoFacturado { get; set; }
    public int Guardados { get; private set; }

    public CategoriaServicio NuevaCategoria(string nombre, EstadoRegistro estado = EstadoRegistro.Activo)
    {
        var c = new CategoriaServicio { Id = Categorias.Count + 1, OrganizacionId = 1, Nombre = nombre, Estado = estado };
        Categorias.Add(c);
        return c;
    }

    public Servicio NuevoServicio(int? categoriaId = null)
    {
        var abonado = new Abonado { Id = 1, OrganizacionId = 1, Codigo = "CB-AB-000001", Identificacion = "102340567", Nombre = "Maria Rojas" };
        var propiedad = abonado.AgregarPropiedad("Casa azul");
        var servicio = propiedad.AgregarServicio(new DateOnly(2025, 1, 1));
        servicio.Id = Servicios.Count + 1;
        servicio.CategoriaId = categoriaId;
        Servicios.Add(servicio);
        return servicio;
    }

    public Task<IReadOnlyList<CategoriaServicio>> ListarCategoriasAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<CategoriaServicio>>(Categorias.OrderBy(c => c.Nombre).ToList());

    public Task<IReadOnlyDictionary<int, int>> ContarServiciosPorCategoriaAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyDictionary<int, int>>(Servicios.Where(s => s.CategoriaId != null)
            .GroupBy(s => s.CategoriaId!.Value).ToDictionary(g => g.Key, g => g.Count()));

    public Task<int> ContarServiciosActivosSinCategoriaAsync(CancellationToken ct = default)
        => Task.FromResult(Servicios.Count(s => s.CategoriaId == null && s.Estado == EstadoRegistro.Activo));

    public Task<CategoriaServicio?> ObtenerCategoriaAsync(int categoriaId, CancellationToken ct = default)
        => Task.FromResult(Categorias.FirstOrDefault(c => c.Id == categoriaId));

    public Task<bool> ExisteCategoriaConNombreAsync(string nombre, CancellationToken ct = default)
        => Task.FromResult(Categorias.Any(c => string.Equals(c.Nombre, nombre, StringComparison.OrdinalIgnoreCase)));

    public Task AgregarCategoriaAsync(CategoriaServicio categoria, CancellationToken ct = default)
    {
        categoria.Id = Categorias.Count + 1;
        Categorias.Add(categoria);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Tarifa>> ListarTarifasAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Tarifa>>(Tarifas.ToList());

    public Task<Tarifa?> ObtenerTarifaAsync(int tarifaId, CancellationToken ct = default)
    {
        var t = Tarifas.FirstOrDefault(x => x.Id == tarifaId);
        if (t is not null) t.Categoria = Categorias.FirstOrDefault(c => c.Id == t.CategoriaId);
        return Task.FromResult(t);
    }

    public Task AgregarTarifaAsync(Tarifa tarifa, CancellationToken ct = default)
    {
        tarifa.Id = Tarifas.Count + 1;
        Tarifas.Add(tarifa);
        return Task.CompletedTask;
    }

    public Task<(int Anio, int Mes)?> ObtenerUltimoPeriodoFacturadoAsync(int categoriaId, CancellationToken ct = default)
        => Task.FromResult(UltimoPeriodoFacturado);

    public Task<Servicio?> ObtenerServicioAsync(int servicioId, CancellationToken ct = default)
        => Task.FromResult(Servicios.FirstOrDefault(s => s.Id == servicioId));

    public Task GuardarCambiosAsync(CancellationToken ct = default) { Guardados++; return Task.CompletedTask; }
}

/// <summary>Repositorio en memoria que reemplaza a RepositorioRecibos (EF Core) en las pruebas.</summary>
public sealed class RepositorioRecibosEnMemoria : IRepositorioRecibos
{
    public List<ServicioAFacturar> Servicios { get; } = [];
    public List<Tarifa> Tarifas { get; } = [];
    public List<Periodo> Periodos { get; } = [];
    public List<Recibo> Recibos { get; } = [];
    public Dictionary<int, int> UltimoCorrelativoPorAnio { get; } = [];
    public bool SimularConflicto { get; set; }

    public static Tarifa TarifaEjemplo(int id, int categoriaId, DateOnly desde, decimal cargoFijo = 2000m) => new()
    {
        Id = id, OrganizacionId = 1, CategoriaId = categoriaId, VigenteDesde = desde, CargoFijo = cargoFijo,
        ModoHidrante = ModoHidrante.PorMetroCubico, ValorHidrante = 10m,
        Bloques =
        [
            new TarifaBloque { Orden = 1, LimiteSuperior = 15m, PrecioPorMetroCubico = 300m },
            new TarifaBloque { Orden = 2, LimiteSuperior = null, PrecioPorMetroCubico = 500m },
        ],
    };

    /// <summary>Servicio con lectura y categoria 1, listo para facturar.</summary>
    public static ServicioAFacturar Listo(int servicioId, decimal consumo = 20m, bool revision = false,
        int? categoriaId = 1, int? medidorAnteriorId = 2) => new(
        servicioId, servicioId, $"CB-AB-{servicioId:000000}", $"Abonado {servicioId}", "Casa",
        categoriaId, categoriaId is null ? null : "Domiciliar",
        MedicionId: 100 + servicioId, Lectura: 120m + consumo, FechaLectura: new DateOnly(2026, 10, 27), Consumo: consumo,
        LecturaRequiereRevision: revision, MotivosRevision: revision ? "Consumo anormal" : null,
        NumeroSerieMedidor: "M-2", MedidorId: 2,
        LecturaAnterior: 120m, MedidorAnteriorId: medidorAnteriorId,
        ReciboId: null, ReciboNumero: null, ReciboTotal: null);

    public static ServicioAFacturar SinLectura(int servicioId) => Listo(servicioId) with
    {
        MedicionId = null, Lectura = null, FechaLectura = null, Consumo = null, NumeroSerieMedidor = null, MedidorId = null,
    };

    public Task<IReadOnlyList<ServicioAFacturar>> ListarServiciosDelPeriodoAsync(int anio, int mes, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<ServicioAFacturar>>(Servicios.ToList());

    public Task<IReadOnlyList<Tarifa>> ListarTarifasAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Tarifa>>(Tarifas.ToList());

    public Task<Periodo?> ObtenerPeriodoAsync(int anio, int mes, CancellationToken ct = default)
        => Task.FromResult(Periodos.FirstOrDefault(p => p.Anio == anio && p.Mes == mes));

    public Task<bool> AgregarRecibosAsync(int anio, IReadOnlyList<Recibo> recibos, CancellationToken ct = default)
    {
        if (SimularConflicto) return Task.FromResult(false);
        foreach (var r in recibos)
        {
            var correlativo = UltimoCorrelativoPorAnio.GetValueOrDefault(anio) + 1;
            UltimoCorrelativoPorAnio[anio] = correlativo;
            r.Id = Recibos.Count + 1;
            r.Correlativo = correlativo;
            r.Numero = ReglasRecibo.GenerarNumero("CB", anio, correlativo);
            Recibos.Add(r);

            // Como en la base de datos: a partir de ahora el servicio aparece con su recibo.
            var i = Servicios.FindIndex(s => s.ServicioId == r.ServicioId);
            Servicios[i] = Servicios[i] with { ReciboId = r.Id, ReciboNumero = r.Numero, ReciboTotal = r.Total };
        }
        return Task.FromResult(true);
    }

    public Task<(Recibo Recibo, Periodo Periodo)?> ObtenerReciboAsync(int reciboId, CancellationToken ct = default)
    {
        var r = Recibos.FirstOrDefault(x => x.Id == reciboId);
        return Task.FromResult<(Recibo, Periodo)?>(r is null ? null : (r, Periodos.First(p => p.Id == r.PeriodoId)));
    }

    public Task<IReadOnlyList<Recibo>> ListarRecibosPeriodoAsync(int anio, int mes, CancellationToken ct = default)
    {
        var periodo = Periodos.FirstOrDefault(p => p.Anio == anio && p.Mes == mes);
        return Task.FromResult<IReadOnlyList<Recibo>>(
            periodo is null ? [] : Recibos.Where(r => r.PeriodoId == periodo.Id).ToList());
    }

    public Task<Organizacion?> ObtenerOrganizacionAsync(int organizacionId, CancellationToken ct = default)
        => Task.FromResult<Organizacion?>(new Organizacion { Id = organizacionId, Codigo = "CB", Nombre = "ASADA Cuatro Bocas" });
}
