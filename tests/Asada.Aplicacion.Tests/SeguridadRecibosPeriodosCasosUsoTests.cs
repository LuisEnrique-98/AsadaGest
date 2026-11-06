using Asada.Aplicacion.Auditoria;
using Asada.Aplicacion.Configuracion;
using Asada.Aplicacion.Periodos;
using Asada.Aplicacion.Periodos.Dtos;
using Asada.Aplicacion.Recibos;
using Asada.Aplicacion.Recibos.Dtos;
using Asada.Dominio.Entidades;
using Asada.Dominio.Enums;
using Xunit;
using static Asada.Aplicacion.Tests.RepositorioRecibosEnMemoria;

namespace Asada.Aplicacion.Tests;

/// <summary>Sprint 5a: anulacion de recibos, quien los genera, cierre y reapertura de periodos.</summary>
public class SeguridadRecibosPeriodosCasosUsoTests
{
    private const string Motivo = "Consumo mal calculado por lectura erronea";
    private static readonly DateOnly Vence = new(2026, 11, 15);
    private readonly RepositorioRecibosEnMemoria repo = new();
    private readonly ProveedorOrganizacionFalso org = new(1);
    private readonly ProveedorUsuarioFalso admin = new(1, "Admin Cuatro Bocas");
    private readonly RepositorioPeriodosEnMemoria periodos;

    public SeguridadRecibosPeriodosCasosUsoTests()
    {
        repo.Periodos.Add(new Periodo { Id = 7, OrganizacionId = 1, Anio = 2026, Mes = 10 });
        repo.Tarifas.Add(TarifaEjemplo(1, categoriaId: 1, new DateOnly(2025, 1, 1)));
        periodos = new RepositorioPeriodosEnMemoria(repo.Periodos);
    }

    private static ProveedorFechaFalso Dia(int mes, int dia) => new(new DateOnly(2026, mes, dia));
    private GenerarRecibosPeriodoCasoUso Generar(ProveedorFechaFalso? fecha = null) => new(repo, org, fecha ?? Dia(10, 30), new ProveedorUsuarioFalso());
    private AnularReciboCasoUso Anular => new(repo, admin);
    private ObtenerCierrePeriodoCasoUso Cierre(ProveedorFechaFalso fecha) => new(periodos, repo, fecha);
    private CerrarPeriodoCasoUso Cerrar(ProveedorFechaFalso fecha) => new(periodos, repo, fecha, org, admin);
    private ReabrirPeriodoCasoUso Reabrir => new(periodos, admin);
    private Periodo Octubre => repo.Periodos.Single(p => p.Mes == 10);

    private async Task GenerarOctubreAsync()
        => Assert.True((await Generar().EjecutarAsync(new GenerarRecibosSolicitud(2026, 10, Vence))).EsExitoso);

    [Fact]
    public async Task Generar_GuardaQuienGeneroElRecibo()
    {
        repo.Servicios.Add(Listo(1));
        await GenerarOctubreAsync();

        var r = Assert.Single(repo.Recibos);
        Assert.Equal(7, r.GeneradoPorUsuarioId);
        Assert.Equal("Ana Operadora", r.GeneradoPorNombre);
        Assert.Equal("Ana Operadora", (await new ObtenerReciboCasoUso(repo).EjecutarAsync(r.Id)).Valor!.GeneradoPorNombre);
    }

    [Fact]
    public async Task Generar_EnPeriodoCerrado_Falla()
    {
        repo.Servicios.Add(Listo(1));
        Octubre.Cerrar(1, "Admin", DateTime.UtcNow);

        var r = await Generar().EjecutarAsync(new GenerarRecibosSolicitud(2026, 10, Vence));

        Assert.False(r.EsExitoso);
        Assert.Contains("cerrado", r.Errores[0]);
        Assert.Empty(repo.Recibos);
    }

    [Fact]
    public async Task AnularRecibo_CambiaEstadoYGuardaMotivo()
    {
        repo.Servicios.Add(Listo(1));
        await GenerarOctubreAsync();

        var r = await Anular.EjecutarAsync(new AnularReciboSolicitud(1, Motivo));

        Assert.True(r.EsExitoso);
        var recibo = repo.Recibos.Single();
        Assert.Equal(EstadoRecibo.Anulado, recibo.Estado);
        Assert.Equal(Motivo, recibo.MotivoAnulacion);
        Assert.Equal("Admin Cuatro Bocas", recibo.AnuladoPorNombre);
        Assert.Equal(1, repo.Guardados);

        var dto = (await new ObtenerReciboCasoUso(repo).EjecutarAsync(1)).Valor!;
        Assert.True(dto.EstaAnulado);
        Assert.Equal(Motivo, dto.MotivoAnulacion);
    }

    [Fact]
    public async Task AnularRecibo_SinMotivo_YaAnulado_OInexistente_Fallan()
    {
        repo.Servicios.Add(Listo(1));
        await GenerarOctubreAsync();

        Assert.False((await Anular.EjecutarAsync(new AnularReciboSolicitud(1, "error"))).EsExitoso);
        Assert.Equal(EstadoRecibo.Emitido, repo.Recibos.Single().Estado);

        Assert.True((await Anular.EjecutarAsync(new AnularReciboSolicitud(1, Motivo))).EsExitoso);
        var segunda = await Anular.EjecutarAsync(new AnularReciboSolicitud(1, Motivo));
        Assert.False(segunda.EsExitoso);
        Assert.Contains("ya esta anulado", segunda.Errores[0]);

        Assert.False((await Anular.EjecutarAsync(new AnularReciboSolicitud(99, Motivo))).EsExitoso);
    }

    [Fact]
    public async Task AnularRecibo_EnPeriodoCerrado_Falla()
    {
        repo.Servicios.Add(Listo(1));
        await GenerarOctubreAsync();
        Octubre.Cerrar(1, "Admin", DateTime.UtcNow);

        var r = await Anular.EjecutarAsync(new AnularReciboSolicitud(1, Motivo));

        Assert.False(r.EsExitoso);
        Assert.Equal(EstadoRecibo.Emitido, repo.Recibos.Single().Estado);
    }

    [Fact]
    public async Task TrasAnular_ElServicioVuelveAFacturarseConOtroNumero()
    {
        repo.Servicios.Add(Listo(1));
        await GenerarOctubreAsync();
        await Anular.EjecutarAsync(new AnularReciboSolicitud(1, Motivo));

        var facturacion = (await new ObtenerFacturacionPeriodoCasoUso(repo).EjecutarAsync(2026, 10)).Valor!;
        Assert.Equal(EstadoFacturacion.ListoParaFacturar, facturacion.Filas.Single().Estado);
        var anulado = Assert.Single(facturacion.Anulados);
        Assert.Equal("CB-REC-2026-000001", anulado.Numero);
        Assert.Equal(Motivo, anulado.MotivoAnulacion);

        await GenerarOctubreAsync();
        Assert.Equal(2, repo.Recibos.Count);
        Assert.Equal("CB-REC-2026-000002", repo.Recibos[1].Numero);
        Assert.Equal(EstadoRecibo.Emitido, repo.Recibos[1].Estado);
    }

    [Fact]
    public async Task Cierre_AntesDeTerminarLaVentana_NoSePuede()
    {
        var d = (await Cierre(Dia(10, 30)).EjecutarAsync(2026, 10)).Valor!;

        Assert.False(d.PuedeCerrarse);
        Assert.Contains("31/10/2026", d.Impedimentos.Single());
        Assert.False((await Cerrar(Dia(10, 30)).EjecutarAsync(2026, 10)).EsExitoso);
    }

    [Fact]
    public async Task Cierre_ConLecturasSinRecibo_NoSePuede()
    {
        repo.Servicios.Add(Listo(1));
        repo.Servicios.Add(Listo(2, categoriaId: null));

        var d = (await Cierre(Dia(11, 2)).EjecutarAsync(2026, 10)).Valor!;
        Assert.Equal(2, d.ConLecturaSinRecibo);
        Assert.Contains("no recibo", d.Impedimentos.Single());

        var r = await Cerrar(Dia(11, 2)).EjecutarAsync(2026, 10);
        Assert.False(r.EsExitoso);
        Assert.False(Octubre.EstaCerrado);
    }

    [Fact]
    public async Task Cierre_ConTodoFacturado_CierraAunqueHayaServiciosSinLectura()
    {
        repo.Servicios.Add(Listo(1));
        repo.Servicios.Add(SinLectura(2));
        await GenerarOctubreAsync();

        var d = (await Cierre(Dia(11, 2)).EjecutarAsync(2026, 10)).Valor!;
        Assert.True(d.PuedeCerrarse);
        Assert.Equal(1, d.ConRecibo);
        Assert.Equal(1, d.SinLectura);

        var r = await Cerrar(Dia(11, 2)).EjecutarAsync(2026, 10);

        Assert.True(r.EsExitoso, string.Join(" ", r.Errores));
        Assert.True(Octubre.EstaCerrado);
        Assert.Equal("Admin Cuatro Bocas", Octubre.CerradoPorNombre);
        Assert.Equal(1, periodos.Guardados);
        Assert.False((await Cerrar(Dia(11, 2)).EjecutarAsync(2026, 10)).EsExitoso); // ya cerrado
    }

    [Fact]
    public async Task Cierre_DeUnMesSinLecturas_CreaElPeriodoCerrado()
    {
        var r = await Cerrar(Dia(11, 2)).EjecutarAsync(2026, 9);

        Assert.True(r.EsExitoso, string.Join(" ", r.Errores));
        Assert.True(repo.Periodos.Single(p => p.Mes == 9).EstaCerrado);
    }

    [Fact]
    public async Task Reabrir_PideMotivoYSoloSiEstaCerrado()
    {
        Assert.False((await Reabrir.EjecutarAsync(new ReabrirPeriodoSolicitud(2026, 10, "Hay que anular un recibo"))).EsExitoso);

        Octubre.Cerrar(1, "Admin", DateTime.UtcNow);
        Assert.False((await Reabrir.EjecutarAsync(new ReabrirPeriodoSolicitud(2026, 10, "abrir"))).EsExitoso);
        Assert.True(Octubre.EstaCerrado);

        var r = await Reabrir.EjecutarAsync(new ReabrirPeriodoSolicitud(2026, 10, "Hay que anular un recibo mal emitido"));
        Assert.True(r.EsExitoso);
        Assert.False(Octubre.EstaCerrado);

        var d = (await Cierre(Dia(11, 2)).EjecutarAsync(2026, 10)).Valor!;
        Assert.Equal("Hay que anular un recibo mal emitido", d.MotivoReapertura);
        Assert.Equal("Admin Cuatro Bocas", d.ReabiertoPorNombre);
    }
}

public class ConfiguracionYAuditoriaCasosUsoTests
{
    private sealed class RepositorioConfiguracionEnMemoria : IRepositorioConfiguracion
    {
        public Organizacion Organizacion { get; } = new() { Id = 1, Codigo = "CB", Nombre = "ASADA Cuatro Bocas" };
        public int Guardados { get; private set; }
        public Task<Organizacion?> ObtenerOrganizacionAsync(int organizacionId, CancellationToken ct = default)
            => Task.FromResult(organizacionId == 1 ? Organizacion : null);
        public Task GuardarCambiosAsync(CancellationToken ct = default) { Guardados++; return Task.CompletedTask; }
    }

    private readonly RepositorioConfiguracionEnMemoria repo = new();
    private ActualizarConfiguracionCasoUso Actualizar => new(repo, new ProveedorOrganizacionFalso(1));

    [Fact]
    public async Task Obtener_DevuelveLosDatosDeLaAsada()
    {
        var r = await new ObtenerConfiguracionCasoUso(repo, new ProveedorOrganizacionFalso(1)).EjecutarAsync();
        Assert.Equal("CB", r.Valor!.Codigo);
        Assert.Equal(50m, r.Valor.PorcentajeConsumoAnormal);
        Assert.False((await new ObtenerConfiguracionCasoUso(repo, new ProveedorOrganizacionFalso(null)).EjecutarAsync()).EsExitoso);
    }

    [Fact]
    public async Task Actualizar_GuardaUmbralYFormateaLaCedulaJuridica()
    {
        var r = await Actualizar.EjecutarAsync(new ActualizarConfiguracionSolicitud(
            "3101123456", " 2222-3333 ", "asada@correo.cr", "Cuatro Bocas, Upala", 80m));

        Assert.True(r.EsExitoso, string.Join(" ", r.Errores));
        Assert.Equal("3-101-123456", repo.Organizacion.CedulaJuridica);
        Assert.Equal("2222-3333", repo.Organizacion.Telefono);
        Assert.Equal(80m, repo.Organizacion.PorcentajeConsumoAnormal);
        Assert.Equal(1, repo.Guardados);
    }

    [Fact]
    public async Task Actualizar_ConDatosInvalidos_NoGuardaNada()
    {
        var r = await Actualizar.EjecutarAsync(new ActualizarConfiguracionSolicitud("123", null, "no-es-correo", null, 5m));

        Assert.False(r.EsExitoso);
        Assert.Equal(3, r.Errores.Count);
        Assert.Equal(0, repo.Guardados);
        Assert.Equal(50m, repo.Organizacion.PorcentajeConsumoAnormal);
    }

    [Fact]
    public void CambiosDeAuditoria_SeSerializanYLeen()
    {
        var json = CambioAuditoria.Serializar([new("Lectura", "120", "125"), new("Observaciones", null, "Revisada")]);
        var cambios = CambioAuditoria.Deserializar(json);

        Assert.Equal(2, cambios.Count);
        Assert.Equal(new CambioAuditoria("Lectura", "120", "125"), cambios[0]);
        Assert.Null(cambios[1].Antes);
        Assert.Empty(CambioAuditoria.Deserializar("no es json"));
    }

    [Fact]
    public async Task BuscarAuditoria_ValidaElFiltro()
    {
        var uso = new BuscarAuditoriaCasoUso(new RepositorioAuditoriaVacio());
        Assert.False((await uso.EjecutarAsync(new FiltroAuditoria(new DateOnly(2026, 10, 5), new DateOnly(2026, 10, 1), null, null, null), 1)).EsExitoso);
        Assert.False((await uso.EjecutarAsync(new FiltroAuditoria(null, null, "Contrasena", null, null), 1)).EsExitoso);
        // Sprint 5b: los usuarios (altas, cambios, inicios de sesion) ya aparecen en la bitacora.
        Assert.True((await uso.EjecutarAsync(new FiltroAuditoria(null, null, "Usuario", 2, null), 1)).EsExitoso);
        Assert.True((await uso.EjecutarAsync(new FiltroAuditoria(null, null, "Recibo", 3, " Ana "), 0)).EsExitoso);
    }

    private sealed class RepositorioAuditoriaVacio : IRepositorioAuditoria
    {
        public Task<Common.PaginaResultado<RegistroAuditoriaDto>> BuscarAsync(FiltroAuditoria filtro, int pagina, int tamanoPagina, CancellationToken ct = default)
            => Task.FromResult(new Common.PaginaResultado<RegistroAuditoriaDto>([], 0, pagina, tamanoPagina));
    }
}
