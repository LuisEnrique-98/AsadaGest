using Asada.Aplicacion.Mediciones;
using Asada.Aplicacion.Mediciones.Dtos;
using Asada.Dominio.Entidades;
using Asada.Dominio.Enums;
using Xunit;

namespace Asada.Aplicacion.Tests;

/// <summary>Escenario comun: un servicio activo de la ASADA 1, "hoy" es el 28/10/2026 (ventana de octubre abierta).</summary>
public class Escenario
{
    public static readonly DateOnly Hoy = new(2026, 10, 28);

    public RepositorioMedicionesEnMemoria Repo { get; } = new();
    public Servicio Servicio { get; }
    public ProveedorFechaFalso Fecha { get; } = new(Hoy);
    public ProveedorOrganizacionFalso Org { get; } = new(1);

    public Escenario() { Servicio = Repo.NuevoServicio(); }

    public InstalarMedidorCasoUso Instalar => new(Repo, Org, Fecha);
    public RetirarMedidorCasoUso Retirar => new(Repo, Fecha);
    public RegistrarLecturaCasoUso Lectura => new(Repo, Org, Fecha);
    public RegistrarSeguimientoLecturaCasoUso Seguimiento => new(Repo, Org);
    public ObtenerLecturasPeriodoCasoUso LecturasPeriodo => new(Repo, Fecha);

    public async Task<int> InstalarMedidorAsync(string serie = "M-1", decimal inicial = 0m, DateOnly? fecha = null)
        => (await Instalar.EjecutarAsync(new InstalarMedidorSolicitud(
            Servicio.Id, serie, fecha ?? new DateOnly(2025, 1, 10), inicial, null))).Valor;

    public Task<Asada.Aplicacion.Common.Resultado<LecturaRegistradaDto>> LeerAsync(int anio, int mes, int dia, decimal lectura)
        => Lectura.EjecutarAsync(new RegistrarLecturaSolicitud(Servicio.Id, anio, mes, new DateOnly(anio, mes, dia), lectura, null));
}

public class MedidoresCasosUsoTests
{
    [Fact]
    public async Task Instalar_CreaElMedidorActivoConLaOrganizacionDelUsuario()
    {
        var e = new Escenario();
        var r = await e.Instalar.EjecutarAsync(new InstalarMedidorSolicitud(e.Servicio.Id, " M-100 ", new DateOnly(2026, 1, 5), 3m, null));

        Assert.True(r.EsExitoso);
        var m = Assert.Single(e.Repo.Medidores);
        Assert.Equal("M-100", m.NumeroSerie);
        Assert.Equal(EstadoMedidor.Activo, m.Estado);
        Assert.Equal(1, m.OrganizacionId);
    }

    [Fact]
    public async Task Instalar_ConOtroActivo_Falla()
    {
        var e = new Escenario();
        await e.InstalarMedidorAsync("M-1");

        var r = await e.Instalar.EjecutarAsync(new InstalarMedidorSolicitud(e.Servicio.Id, "M-2", new DateOnly(2026, 1, 5), 0m, null));

        Assert.False(r.EsExitoso);
        Assert.Contains(r.Errores, x => x.Contains("medidor activo"));
    }

    [Fact]
    public async Task Instalar_ConSerieRepetida_Falla()
    {
        var e = new Escenario();
        var id = await e.InstalarMedidorAsync("M-1");
        await e.Retirar.EjecutarAsync(new RetirarMedidorSolicitud(id, new DateOnly(2026, 2, 1), 10m));

        var r = await e.Instalar.EjecutarAsync(new InstalarMedidorSolicitud(e.Servicio.Id, "M-1", new DateOnly(2026, 3, 1), 0m, null));

        Assert.False(r.EsExitoso);
        Assert.Contains(r.Errores, x => x.Contains("numero de serie"));
    }

    [Fact]
    public async Task Instalar_FechaFuturaOSerieVacia_Falla()
    {
        var e = new Escenario();
        var r = await e.Instalar.EjecutarAsync(new InstalarMedidorSolicitud(e.Servicio.Id, " ", Escenario.Hoy.AddDays(1), 0m, null));

        Assert.False(r.EsExitoso);
        Assert.Equal(2, r.Errores.Count);
        Assert.Empty(e.Repo.Medidores);
    }

    [Fact]
    public async Task Instalar_AntesDelRetiroDelAnterior_Falla()
    {
        var e = new Escenario();
        var id = await e.InstalarMedidorAsync("M-1");
        await e.Retirar.EjecutarAsync(new RetirarMedidorSolicitud(id, new DateOnly(2026, 6, 10), 50m));

        var r = await e.Instalar.EjecutarAsync(new InstalarMedidorSolicitud(e.Servicio.Id, "M-2", new DateOnly(2026, 6, 9), 0m, null));

        Assert.False(r.EsExitoso);
    }

    [Fact]
    public async Task Instalar_EnServicioInactivoOSinOrganizacion_Falla()
    {
        var e = new Escenario();
        e.Servicio.Estado = EstadoRegistro.Inactivo;
        var solicitud = new InstalarMedidorSolicitud(e.Servicio.Id, "M-1", new DateOnly(2026, 1, 1), 0m, null);

        Assert.False((await e.Instalar.EjecutarAsync(solicitud)).EsExitoso);
        Assert.False((await new InstalarMedidorCasoUso(e.Repo, new ProveedorOrganizacionFalso(null), e.Fecha).EjecutarAsync(solicitud)).EsExitoso);
    }

    [Fact]
    public async Task Retirar_CierraElMedidorConSuLecturaFinal()
    {
        var e = new Escenario();
        var id = await e.InstalarMedidorAsync();

        var r = await e.Retirar.EjecutarAsync(new RetirarMedidorSolicitud(id, new DateOnly(2026, 6, 1), 320m));

        Assert.True(r.EsExitoso);
        Assert.Equal(EstadoMedidor.Retirado, e.Repo.Medidores[0].Estado);
        Assert.Equal(320m, e.Repo.Medidores[0].LecturaFinal);
        Assert.Equal(1, e.Repo.Guardados);
    }

    [Fact]
    public async Task Retirar_ConLecturaFinalMenorQueLaUltimaLectura_Falla()
    {
        var e = new Escenario();
        var id = await e.InstalarMedidorAsync();
        await e.LeerAsync(2026, 9, 28, 100m);

        var r = await e.Retirar.EjecutarAsync(new RetirarMedidorSolicitud(id, new DateOnly(2026, 10, 2), 90m));

        Assert.False(r.EsExitoso);
        Assert.Contains(r.Errores, x => x.Contains("ultima lectura"));
        Assert.Equal(EstadoMedidor.Activo, e.Repo.Medidores[0].Estado);
    }

    [Fact]
    public async Task Retirar_YaRetiradoOInexistenteOFuturo_Falla()
    {
        var e = new Escenario();
        var id = await e.InstalarMedidorAsync();
        await e.Retirar.EjecutarAsync(new RetirarMedidorSolicitud(id, new DateOnly(2026, 6, 1), 20m));

        Assert.False((await e.Retirar.EjecutarAsync(new RetirarMedidorSolicitud(id, new DateOnly(2026, 6, 2), 30m))).EsExitoso);
        Assert.False((await e.Retirar.EjecutarAsync(new RetirarMedidorSolicitud(999, new DateOnly(2026, 6, 2), 30m))).EsExitoso);
        Assert.False((await e.Retirar.EjecutarAsync(new RetirarMedidorSolicitud(id, Escenario.Hoy.AddDays(1), 30m))).EsExitoso);
    }

    [Fact]
    public async Task ObtenerMedidores_DevuelveElHistorialYElActivo()
    {
        var e = new Escenario();
        var id = await e.InstalarMedidorAsync("M-1");
        await e.Retirar.EjecutarAsync(new RetirarMedidorSolicitud(id, new DateOnly(2026, 2, 1), 40m));
        await e.Instalar.EjecutarAsync(new InstalarMedidorSolicitud(e.Servicio.Id, "M-2", new DateOnly(2026, 2, 1), 0m, null));

        var r = await new ObtenerMedidoresServicioCasoUso(e.Repo).EjecutarAsync(e.Servicio.Id);

        Assert.True(r.EsExitoso);
        Assert.Equal(2, r.Valor!.Medidores.Count);
        Assert.Equal("M-2", r.Valor.MedidorActivo!.NumeroSerie);
        Assert.Equal("CB-AB-000001", r.Valor.AbonadoCodigo);
    }
}

public class RegistrarLecturaCasoUsoTests
{
    [Fact]
    public async Task PrimeraLectura_EnVentana_UsaLaLecturaInicialYNoRequiereRevision()
    {
        var e = new Escenario();
        await e.InstalarMedidorAsync(inicial: 5m);

        var r = await e.LeerAsync(2026, 10, 27, 17m);

        Assert.True(r.EsExitoso);
        Assert.Equal(12m, r.Valor!.Consumo);
        Assert.False(r.Valor.RequiereRevision);
        Assert.Equal(1, e.Repo.Mediciones[0].PeriodoId);
    }

    [Fact]
    public async Task LecturaConsecutiva_CalculaElConsumoContraLaAnterior()
    {
        var e = new Escenario();
        await e.InstalarMedidorAsync();
        await e.LeerAsync(2026, 9, 27, 100m);

        var r = await e.LeerAsync(2026, 10, 27, 118m);

        Assert.Equal(18m, r.Valor!.Consumo);
        Assert.False(r.Valor.RequiereRevision);
    }

    [Fact]
    public async Task LecturaFueraDeLaVentana_SeGuardaYQuedaParaRevision()
    {
        var e = new Escenario();
        await e.InstalarMedidorAsync();

        // Periodo de septiembre leido el 3 de octubre: ya paso la ventana (24 al 30 de septiembre).
        var r = await e.Lectura.EjecutarAsync(new RegistrarLecturaSolicitud(e.Servicio.Id, 2026, 9, new DateOnly(2026, 10, 3), 40m, null));

        Assert.True(r.EsExitoso);
        Assert.True(r.Valor!.RequiereRevision);
        Assert.Contains(r.Valor.MotivosRevision, m => m.Contains("fuera de la ventana"));
    }

    [Fact]
    public async Task RB014_PeriodoAnteriorSinLectura_FacturaCompletoYMarcaRevision()
    {
        var e = new Escenario();
        await e.InstalarMedidorAsync();
        await e.LeerAsync(2026, 8, 28, 100m);
        // Septiembre queda sin lectura.

        var r = await e.LeerAsync(2026, 10, 27, 130m);

        Assert.True(r.EsExitoso);
        Assert.Equal(30m, r.Valor!.Consumo);   // no se reparte entre meses
        Assert.True(r.Valor.RequiereRevision);
        Assert.Contains(r.Valor.MotivosRevision, m => m.Contains("anterior quedo sin lectura"));
    }

    [Fact]
    public async Task ConsumoMuySuperiorAlPromedio_QuedaParaRevisionSinBloquearse()
    {
        var e = new Escenario();
        await e.InstalarMedidorAsync();
        await e.LeerAsync(2026, 6, 27, 10m);
        await e.LeerAsync(2026, 7, 27, 20m);
        await e.LeerAsync(2026, 8, 27, 30m);
        await e.LeerAsync(2026, 9, 27, 40m);   // consumos previos: 10, 10, 10

        var r = await e.LeerAsync(2026, 10, 27, 80m);   // consumo 40, promedio 10

        Assert.True(r.EsExitoso);
        Assert.Equal(40m, r.Valor!.Consumo);
        Assert.True(r.Valor.RequiereRevision);
        Assert.Contains(r.Valor.MotivosRevision, m => m.Contains("promedio"));
    }

    [Fact]
    public async Task RB013_CambioDeMedidorEntreLecturas_SumaLosDosTramos()
    {
        var e = new Escenario();
        var viejo = await e.InstalarMedidorAsync("VIEJO");
        await e.LeerAsync(2026, 8, 28, 300m);

        await e.Retirar.EjecutarAsync(new RetirarMedidorSolicitud(viejo, new DateOnly(2026, 9, 10), 320m));
        await e.Instalar.EjecutarAsync(new InstalarMedidorSolicitud(e.Servicio.Id, "NUEVO", new DateOnly(2026, 9, 10), 0m, null));

        var r = await e.LeerAsync(2026, 9, 27, 8m);

        Assert.True(r.EsExitoso);
        Assert.Equal(28m, r.Valor!.Consumo);   // (320-300) + (8-0)
        Assert.False(r.Valor.RequiereRevision);
        Assert.Equal(2, e.Repo.Mediciones[1].MedidorId);
    }

    [Fact]
    public async Task LecturaMenorQueLaAnterior_ConElMismoMedidor_SeRechaza()
    {
        var e = new Escenario();
        await e.InstalarMedidorAsync();
        await e.LeerAsync(2026, 9, 27, 100m);

        var r = await e.LeerAsync(2026, 10, 27, 90m);

        Assert.False(r.EsExitoso);
        Assert.Single(e.Repo.Mediciones);
    }

    [Fact]
    public async Task UnaSolaLecturaPorPeriodo_YEnOrdenCronologico()
    {
        var e = new Escenario();
        await e.InstalarMedidorAsync();
        await e.LeerAsync(2026, 9, 27, 100m);

        Assert.False((await e.LeerAsync(2026, 9, 28, 105m)).EsExitoso);   // mismo periodo
        Assert.False((await e.LeerAsync(2026, 8, 27, 90m)).EsExitoso);    // periodo anterior a la ultima lectura
        Assert.Single(e.Repo.Mediciones);
    }

    [Fact]
    public async Task SinMedidorActivo_SeRechaza()
    {
        var e = new Escenario();

        var r = await e.LeerAsync(2026, 10, 27, 10m);

        Assert.False(r.EsExitoso);
        Assert.Contains(r.Errores, x => x.Contains("medidor"));
    }

    [Fact]
    public async Task FechaFutura_FechaAntesDeLaInstalacionYLecturaNegativa_SeRechazan()
    {
        var e = new Escenario();
        await e.InstalarMedidorAsync(fecha: new DateOnly(2026, 10, 1));

        Assert.False((await e.Lectura.EjecutarAsync(new RegistrarLecturaSolicitud(e.Servicio.Id, 2026, 10, Escenario.Hoy.AddDays(1), 10m, null))).EsExitoso);
        Assert.False((await e.Lectura.EjecutarAsync(new RegistrarLecturaSolicitud(e.Servicio.Id, 2026, 9, new DateOnly(2026, 9, 27), 10m, null))).EsExitoso);
        Assert.False((await e.LeerAsync(2026, 10, 27, -1m)).EsExitoso);
        Assert.Empty(e.Repo.Mediciones);
    }

    [Fact]
    public async Task PeriodoInvalido_ServicioInexistenteOSinOrganizacion_SeRechazan()
    {
        var e = new Escenario();
        await e.InstalarMedidorAsync();

        Assert.False((await e.Lectura.EjecutarAsync(new RegistrarLecturaSolicitud(e.Servicio.Id, 2026, 13, new DateOnly(2026, 10, 27), 10m, null))).EsExitoso);
        Assert.False((await e.Lectura.EjecutarAsync(new RegistrarLecturaSolicitud(999, 2026, 10, new DateOnly(2026, 10, 27), 10m, null))).EsExitoso);
        var sinOrg = new RegistrarLecturaCasoUso(e.Repo, new ProveedorOrganizacionFalso(null), e.Fecha);
        Assert.False((await sinOrg.EjecutarAsync(new RegistrarLecturaSolicitud(e.Servicio.Id, 2026, 10, new DateOnly(2026, 10, 27), 10m, null))).EsExitoso);
    }
}

public class LecturasPendientesCasosUsoTests
{
    [Fact]
    public async Task ConLaVentanaCerrada_LosServiciosSinLecturaGeneranAlerta()
    {
        var e = new Escenario();                       // hoy = 28/10: la ventana de septiembre ya cerro
        await e.InstalarMedidorAsync();

        var r = await e.LecturasPeriodo.EjecutarAsync(2026, 9);

        Assert.True(r.EsExitoso);
        Assert.Equal(EstadoVentanaLectura.Cerrada, r.Valor!.EstadoVentana);
        Assert.Equal(1, r.Valor.SinLectura);
        Assert.True(r.Valor.HayAlerta);
    }

    [Fact]
    public async Task ConLaVentanaAbierta_SinLecturaTodaviaNoEsAlerta()
    {
        var e = new Escenario();
        await e.InstalarMedidorAsync();

        var r = await e.LecturasPeriodo.EjecutarAsync(2026, 10);

        Assert.Equal(EstadoVentanaLectura.Abierta, r.Valor!.EstadoVentana);
        Assert.Equal(1, r.Valor.SinLectura);
        Assert.False(r.Valor.HayAlerta);
    }

    [Fact]
    public async Task ServicioConLectura_NoCuentaComoPendiente()
    {
        var e = new Escenario();
        await e.InstalarMedidorAsync();
        await e.LeerAsync(2026, 9, 27, 20m);

        var r = await e.LecturasPeriodo.EjecutarAsync(2026, 9);

        Assert.Equal(1, r.Valor!.ConLectura);
        Assert.False(r.Valor.HayAlerta);
    }

    [Fact]
    public async Task PeriodoInvalido_Falla()
        => Assert.False((await new Escenario().LecturasPeriodo.EjecutarAsync(2026, 0)).EsExitoso);
}

public class SeguimientoLecturaCasoUsoTests
{
    private static RegistrarSeguimientoLecturaSolicitud Solicitud(int servicioId, DecisionSeguimientoLectura d, string? nota = null)
        => new(servicioId, 2026, 9, d, nota);

    [Fact]
    public async Task Crea_LaAnotacionDelServicioSinLectura()
    {
        var e = new Escenario();

        var r = await e.Seguimiento.EjecutarAsync(Solicitud(e.Servicio.Id, DecisionSeguimientoLectura.LeerLoAntesPosible, " Casa cerrada "));

        Assert.True(r.EsExitoso);
        var s = Assert.Single(e.Repo.Seguimientos);
        Assert.Equal("Casa cerrada", s.Nota);
        Assert.Equal(DecisionSeguimientoLectura.LeerLoAntesPosible, s.Decision);
    }

    [Fact]
    public async Task UnaSegundaAnotacion_ActualizaLaExistente()
    {
        var e = new Escenario();
        await e.Seguimiento.EjecutarAsync(Solicitud(e.Servicio.Id, DecisionSeguimientoLectura.LeerLoAntesPosible));

        await e.Seguimiento.EjecutarAsync(Solicitud(e.Servicio.Id, DecisionSeguimientoLectura.EsperarAlSiguienteMes, "Viaje"));

        var s = Assert.Single(e.Repo.Seguimientos);
        Assert.Equal(DecisionSeguimientoLectura.EsperarAlSiguienteMes, s.Decision);
        Assert.Equal("Viaje", s.Nota);
    }

    [Fact]
    public async Task NoSePuedeAnotar_UnServicioQueYaTieneLectura()
    {
        var e = new Escenario();
        await e.InstalarMedidorAsync();
        await e.LeerAsync(2026, 9, 27, 20m);

        var r = await e.Seguimiento.EjecutarAsync(Solicitud(e.Servicio.Id, DecisionSeguimientoLectura.LeerLoAntesPosible));

        Assert.False(r.EsExitoso);
        Assert.Empty(e.Repo.Seguimientos);
    }

    [Fact]
    public async Task LaAnotacion_AparecenEnElListadoDelPeriodo()
    {
        var e = new Escenario();
        await e.InstalarMedidorAsync();
        await e.Seguimiento.EjecutarAsync(Solicitud(e.Servicio.Id, DecisionSeguimientoLectura.EsperarAlSiguienteMes, "Viaje"));

        var fila = (await e.LecturasPeriodo.EjecutarAsync(2026, 9)).Valor!.Filas[0];

        Assert.False(fila.TieneLectura);
        Assert.Equal(DecisionSeguimientoLectura.EsperarAlSiguienteMes, fila.Decision);
        Assert.Equal("Viaje", fila.NotaSeguimiento);
    }

    [Fact]
    public async Task ServicioInexistente_DecisionInvalidaOSinOrganizacion_Falla()
    {
        var e = new Escenario();

        Assert.False((await e.Seguimiento.EjecutarAsync(Solicitud(999, DecisionSeguimientoLectura.LeerLoAntesPosible))).EsExitoso);
        Assert.False((await e.Seguimiento.EjecutarAsync(Solicitud(e.Servicio.Id, (DecisionSeguimientoLectura)9))).EsExitoso);
        var sinOrg = new RegistrarSeguimientoLecturaCasoUso(e.Repo, new ProveedorOrganizacionFalso(null));
        Assert.False((await sinOrg.EjecutarAsync(Solicitud(e.Servicio.Id, DecisionSeguimientoLectura.LeerLoAntesPosible))).EsExitoso);
    }
}
