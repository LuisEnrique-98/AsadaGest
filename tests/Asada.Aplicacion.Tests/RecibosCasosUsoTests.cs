using Asada.Aplicacion.Recibos;
using Asada.Aplicacion.Recibos.Dtos;
using Asada.Dominio.Entidades;
using Asada.Dominio.Enums;
using Xunit;
using static Asada.Aplicacion.Tests.RepositorioRecibosEnMemoria;

namespace Asada.Aplicacion.Tests;

public class RecibosCasosUsoTests
{
    private static readonly DateOnly Hoy = new(2026, 10, 30);
    private static readonly DateOnly Vence = new(2026, 11, 15);
    private readonly RepositorioRecibosEnMemoria repo = new();
    private readonly ProveedorOrganizacionFalso org = new(1);
    private readonly ProveedorFechaFalso fecha = new(Hoy);

    public RecibosCasosUsoTests()
    {
        repo.Periodos.Add(new Periodo { Id = 7, OrganizacionId = 1, Anio = 2026, Mes = 10 });
        repo.Tarifas.Add(TarifaEjemplo(1, categoriaId: 1, new DateOnly(2025, 1, 1)));
    }

    private GenerarRecibosPeriodoCasoUso Generar => new(repo, org, fecha);
    private ObtenerFacturacionPeriodoCasoUso Facturacion => new(repo);

    private Task<Common.Resultado<GeneracionRecibosDto>> GenerarOctubreAsync()
        => Generar.EjecutarAsync(new GenerarRecibosSolicitud(2026, 10, Vence));

    [Fact]
    public async Task Facturacion_ClasificaCadaServicio()
    {
        repo.Servicios.Add(Listo(1));
        repo.Servicios.Add(SinLectura(2));
        repo.Servicios.Add(Listo(3, categoriaId: null));
        repo.Servicios.Add(Listo(4, categoriaId: 2)); // categoria sin tarifa
        repo.Servicios.Add(Listo(5, revision: true));

        var r = await Facturacion.EjecutarAsync(2026, 10);
        Assert.True(r.EsExitoso);
        var d = r.Valor!;
        Assert.Equal(EstadoFacturacion.ListoParaFacturar, d.Filas[0].Estado);
        Assert.Equal(EstadoFacturacion.SinLectura, d.Filas[1].Estado);
        Assert.Equal(EstadoFacturacion.SinCategoria, d.Filas[2].Estado);
        Assert.Equal(EstadoFacturacion.SinTarifa, d.Filas[3].Estado);
        Assert.Equal(2, d.Listos);
        Assert.Equal(1, d.ListosParaRevision);
    }

    [Fact]
    public async Task Facturacion_TarifaQueEntraARegirDespuesDelPeriodo_NoCuenta()
    {
        repo.Tarifas.Clear();
        repo.Tarifas.Add(TarifaEjemplo(1, 1, new DateOnly(2026, 11, 1)));
        repo.Servicios.Add(Listo(1));
        Assert.Equal(EstadoFacturacion.SinTarifa, (await Facturacion.EjecutarAsync(2026, 10)).Valor!.Filas[0].Estado);
    }

    [Fact]
    public async Task Generar_SoloFacturaLosServiciosListos()
    {
        repo.Servicios.Add(Listo(1));
        repo.Servicios.Add(SinLectura(2));
        repo.Servicios.Add(Listo(3, categoriaId: null));

        var r = await GenerarOctubreAsync();
        Assert.True(r.EsExitoso);
        Assert.Equal(1, r.Valor!.Generados);
        Assert.Equal(2, r.Valor.Pendientes);
        Assert.Equal(1, Assert.Single(repo.Recibos).ServicioId);
    }

    [Fact]
    public async Task Generar_CalculaYCongelaElRecibo()
    {
        repo.Servicios.Add(Listo(1, consumo: 20m));
        await GenerarOctubreAsync();

        var recibo = Assert.Single(repo.Recibos);
        // Cargo fijo 2000 + 15 x 300 + 5 x 500 + hidrantes 20 x 10.
        Assert.Equal(2000m + 4500m + 2500m, recibo.SubtotalAgua);
        Assert.Equal(200m, recibo.MontoHidrante);
        Assert.Equal(9200m, recibo.Total);
        Assert.Equal(4, recibo.Detalles.Count);
        Assert.Equal(recibo.Total, recibo.Detalles.Sum(d => d.Monto));
        Assert.Equal("CB-REC-2026-000001", recibo.Numero);
        Assert.Equal(1, recibo.TarifaId);
        Assert.Equal(7, recibo.PeriodoId);
        Assert.Equal(101, recibo.MedicionId);
        Assert.Equal(Hoy, recibo.FechaEmision);
        Assert.Equal(Vence, recibo.FechaVencimiento);
        Assert.Equal("Domiciliar", recibo.CategoriaNombre);
        Assert.Equal(120m, recibo.LecturaAnterior);
        Assert.Equal(140m, recibo.LecturaActual);
        Assert.False(recibo.HuboCambioMedidor);
        Assert.Equal(EstadoRecibo.Emitido, recibo.Estado);
        Assert.Equal(1, recibo.OrganizacionId);
    }

    [Fact]
    public async Task Generar_UsaLaVersionDeTarifaQueRigeEnElPeriodo()
    {
        repo.Tarifas.Add(TarifaEjemplo(2, 1, new DateOnly(2026, 10, 15), cargoFijo: 2500m));
        repo.Tarifas.Add(TarifaEjemplo(3, 1, new DateOnly(2026, 11, 1), cargoFijo: 9999m));
        repo.Servicios.Add(Listo(1));
        await GenerarOctubreAsync();
        Assert.Equal(2, repo.Recibos[0].TarifaId);
    }

    [Fact]
    public async Task Generar_LecturaParaRevision_SeFacturaYQuedaSenalada()
    {
        repo.Servicios.Add(Listo(1, revision: true));
        var r = await GenerarOctubreAsync();
        Assert.Equal(1, r.Valor!.ParaRevision);
        Assert.True(repo.Recibos[0].LecturaRequiereRevision);
        Assert.Equal("Consumo anormal", repo.Recibos[0].MotivosRevision);
    }

    [Fact]
    public async Task Generar_MarcaElCambioDeMedidor()
    {
        repo.Servicios.Add(Listo(1, medidorAnteriorId: 1) with { MedidorId = 2 });
        repo.Servicios.Add(Listo(2, medidorAnteriorId: 2) with { MedidorId = 2 });
        repo.Servicios.Add(Listo(3, medidorAnteriorId: null) with { LecturaAnterior = null });
        await GenerarOctubreAsync();
        Assert.True(repo.Recibos[0].HuboCambioMedidor);
        Assert.False(repo.Recibos[1].HuboCambioMedidor);
        Assert.False(repo.Recibos[2].HuboCambioMedidor);
        Assert.Null(repo.Recibos[2].LecturaAnterior);
    }

    [Fact]
    public async Task Generar_DosVeces_NoDuplicaYFacturaLosQueSePusieronAlDia()
    {
        repo.Servicios.Add(Listo(1));
        repo.Servicios.Add(SinLectura(2));
        await GenerarOctubreAsync();

        repo.Servicios[1] = Listo(2);
        var segunda = await GenerarOctubreAsync();

        Assert.Equal(1, segunda.Valor!.Generados);
        Assert.Equal(2, repo.Recibos.Count);
        Assert.Equal("CB-REC-2026-000002", repo.Recibos[1].Numero);

        var tercera = await GenerarOctubreAsync();
        Assert.Equal(0, tercera.Valor!.Generados);
        Assert.Equal(2, repo.Recibos.Count);
    }

    [Fact]
    public async Task Generar_SinPeriodo_Falla()
    {
        repo.Servicios.Add(Listo(1));
        var r = await Generar.EjecutarAsync(new GenerarRecibosSolicitud(2026, 9, Vence));
        Assert.False(r.EsExitoso);
        Assert.Empty(repo.Recibos);
    }

    [Fact]
    public async Task Generar_VencimientoAnteriorAHoy_Falla()
    {
        repo.Servicios.Add(Listo(1));
        Assert.False((await Generar.EjecutarAsync(new GenerarRecibosSolicitud(2026, 10, Hoy.AddDays(-1)))).EsExitoso);
        Assert.Empty(repo.Recibos);
    }

    [Fact]
    public async Task Generar_Superadministrador_Falla()
        => Assert.False((await new GenerarRecibosPeriodoCasoUso(repo, new ProveedorOrganizacionFalso(null), fecha)
            .EjecutarAsync(new GenerarRecibosSolicitud(2026, 10, Vence))).EsExitoso);

    [Fact]
    public async Task Generar_ConflictoConOtroUsuario_NoGuardaNada()
    {
        repo.Servicios.Add(Listo(1));
        repo.SimularConflicto = true;
        var r = await GenerarOctubreAsync();
        Assert.False(r.EsExitoso);
        Assert.Contains("al mismo tiempo", r.Errores[0]);
        Assert.Empty(repo.Recibos);
    }

    [Fact]
    public async Task Generar_SinNadaListo_DevuelveCero()
    {
        repo.Servicios.Add(SinLectura(1));
        var r = await GenerarOctubreAsync();
        Assert.True(r.EsExitoso);
        Assert.Equal(0, r.Valor!.Generados);
        Assert.Equal(1, r.Valor.Pendientes);
    }

    [Fact]
    public async Task Facturacion_DespuesDeGenerar_MuestraElRecibo()
    {
        repo.Servicios.Add(Listo(1));
        await GenerarOctubreAsync();
        var d = (await Facturacion.EjecutarAsync(2026, 10)).Valor!;
        Assert.Equal(EstadoFacturacion.ConRecibo, d.Filas[0].Estado);
        Assert.Equal("CB-REC-2026-000001", d.Filas[0].ReciboNumero);
        Assert.Equal(9200m, d.MontoEmitido);
    }

    [Fact]
    public async Task ObtenerRecibo_DevuelveElDesgloseEnOrdenYElPeriodo()
    {
        repo.Servicios.Add(Listo(1));
        await GenerarOctubreAsync();
        var r = await new ObtenerReciboCasoUso(repo).EjecutarAsync(1);
        Assert.True(r.EsExitoso);
        Assert.Equal(10, r.Valor!.Mes);
        Assert.Equal("ASADA Cuatro Bocas", r.Valor.OrganizacionNombre);
        Assert.Equal(TipoLineaRecibo.CargoFijo, r.Valor.Lineas[0].Tipo);
        Assert.Equal(TipoLineaRecibo.Hidrante, r.Valor.Lineas[^1].Tipo);
    }

    [Fact]
    public async Task ObtenerRecibo_Inexistente_Falla()
        => Assert.False((await new ObtenerReciboCasoUso(repo).EjecutarAsync(99)).EsExitoso);
}
