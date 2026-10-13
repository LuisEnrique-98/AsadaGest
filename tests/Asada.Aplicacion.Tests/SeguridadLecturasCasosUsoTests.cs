using Asada.Aplicacion.Mediciones;
using Asada.Aplicacion.Mediciones.Dtos;
using Asada.Dominio.Entidades;
using Asada.Dominio.Enums;
using Xunit;

namespace Asada.Aplicacion.Tests;

/// <summary>Sprint 5a: quien registra, umbral por ASADA, periodo cerrado, correccion y anulacion de lecturas.</summary>
public class SeguridadLecturasCasosUsoTests
{
    private const string Motivo = "El lector anoto mal un digito";

    private static CorregirLecturaCasoUso Corregir(Escenario e) => new(e.Repo, e.Fecha, new ProveedorUsuarioFalso(1, "Admin Cuatro Bocas"));
    private static AnularLecturaCasoUso Anular(Escenario e) => new(e.Repo, new ProveedorUsuarioFalso(1, "Admin Cuatro Bocas"));

    private static async Task<int> LeerOkAsync(Escenario e, int anio, int mes, int dia, decimal lectura)
    {
        var r = await e.LeerAsync(anio, mes, dia, lectura);
        Assert.True(r.EsExitoso, string.Join(" ", r.Errores));
        return r.Valor!.MedicionId;
    }

    private static void CerrarPeriodo(Escenario e, int anio, int mes)
        => e.Repo.Periodos.First(p => p.Anio == anio && p.Mes == mes).Cerrar(1, "Admin", DateTime.UtcNow);

    [Fact]
    public async Task Registrar_GuardaQuienRegistroLaLectura()
    {
        var e = new Escenario();
        await e.InstalarMedidorAsync();
        var id = await LeerOkAsync(e, 2026, 10, 27, 15m);

        var m = e.Repo.Mediciones.Single(x => x.Id == id);
        Assert.Equal(7, m.RegistradaPorUsuarioId);
        Assert.Equal("Ana Operadora", m.RegistradaPorNombre);

        var fila = Assert.Single((await e.LecturasPeriodo.EjecutarAsync(2026, 10)).Valor!.Filas);
        Assert.Equal(id, fila.MedicionId);
        Assert.Equal("Ana Operadora", fila.RegistradaPorNombre);
    }

    [Theory]
    [InlineData(50, true)]
    [InlineData(200, false)]
    public async Task Registrar_UsaElUmbralDeConsumoAnormalDeLaAsada(int umbral, bool esperaRevision)
    {
        var e = new Escenario();
        e.Repo.PorcentajeConsumoAnormal = umbral;
        await e.InstalarMedidorAsync();
        await LeerOkAsync(e, 2026, 7, 27, 10m);
        await LeerOkAsync(e, 2026, 8, 27, 20m);
        await LeerOkAsync(e, 2026, 9, 27, 30m);

        // Promedio 10 m3; consumo 25 m3 = 150 % por encima.
        var r = await e.LeerAsync(2026, 10, 27, 55m);

        Assert.True(r.EsExitoso);
        Assert.Equal(esperaRevision, r.Valor!.RequiereRevision);
    }

    [Fact]
    public async Task Registrar_EnPeriodoCerrado_Falla()
    {
        var e = new Escenario();
        await e.InstalarMedidorAsync();
        await e.Repo.ObtenerOCrearPeriodoAsync(2026, 10);
        CerrarPeriodo(e, 2026, 10);

        var r = await e.LeerAsync(2026, 10, 27, 15m);

        Assert.False(r.EsExitoso);
        Assert.Contains("cerrado", r.Errores[0]);
        Assert.Empty(e.Repo.Mediciones);
    }

    [Fact]
    public async Task Seguimiento_EnPeriodoCerrado_Falla()
    {
        var e = new Escenario();
        await e.InstalarMedidorAsync();
        await e.Repo.ObtenerOCrearPeriodoAsync(2026, 9);
        CerrarPeriodo(e, 2026, 9);

        var r = await e.Seguimiento.EjecutarAsync(new RegistrarSeguimientoLecturaSolicitud(
            e.Servicio.Id, 2026, 9, DecisionSeguimientoLectura.LeerLoAntesPosible, null));

        Assert.False(r.EsExitoso);
        Assert.Contains("cerrado", r.Errores[0]);
    }

    [Fact]
    public async Task Lecturas_InformaElEstadoDelPeriodo()
    {
        var e = new Escenario();
        await e.InstalarMedidorAsync();
        await LeerOkAsync(e, 2026, 9, 27, 10m);
        Assert.False((await e.LecturasPeriodo.EjecutarAsync(2026, 9)).Valor!.PeriodoCerrado);

        CerrarPeriodo(e, 2026, 9);
        Assert.True((await e.LecturasPeriodo.EjecutarAsync(2026, 9)).Valor!.PeriodoCerrado);
        Assert.False((await e.LecturasPeriodo.EjecutarAsync(2026, 8)).Valor!.PeriodoCerrado);
    }

    [Fact]
    public async Task Corregir_RecalculaElConsumoYDejaConstancia()
    {
        var e = new Escenario();
        await e.InstalarMedidorAsync();
        await LeerOkAsync(e, 2026, 9, 27, 100m);
        var id = await LeerOkAsync(e, 2026, 10, 27, 150m);

        var r = await Corregir(e).EjecutarAsync(new CorregirLecturaSolicitud(id, new DateOnly(2026, 10, 27), 115m, "Revisada en sitio", Motivo));

        Assert.True(r.EsExitoso, string.Join(" ", r.Errores));
        Assert.Equal(15m, r.Valor!.Consumo);
        var m = e.Repo.Mediciones.Single(x => x.Id == id);
        Assert.Equal(115m, m.Lectura);
        Assert.Equal(15m, m.Consumo);
        Assert.Equal("Revisada en sitio", m.Observaciones);
        Assert.Equal(Motivo, m.MotivoCorreccion);
        Assert.Equal("Admin Cuatro Bocas", m.CorregidaPorNombre);
        Assert.Equal("Ana Operadora", m.RegistradaPorNombre); // quien la registro no cambia
        Assert.Equal(1, e.Repo.Guardados);
    }

    [Fact]
    public async Task Corregir_RecalculaLosMotivosDeRevision()
    {
        var e = new Escenario();
        await e.InstalarMedidorAsync();
        await LeerOkAsync(e, 2026, 9, 27, 100m);
        var registrada = await e.LeerAsync(2026, 10, 10, 110m); // fuera de la ventana
        Assert.True(registrada.Valor!.RequiereRevision);

        var r = await Corregir(e).EjecutarAsync(new CorregirLecturaSolicitud(
            registrada.Valor.MedicionId, new DateOnly(2026, 10, 26), 110m, null, "La fecha se digito mal en el sistema"));

        Assert.True(r.EsExitoso);
        Assert.False(r.Valor!.RequiereRevision);
        Assert.Null(e.Repo.Mediciones.Single(x => x.Id == registrada.Valor.MedicionId).MotivosRevision);
    }

    [Fact]
    public async Task Corregir_SinMotivo_Falla()
    {
        var e = new Escenario();
        await e.InstalarMedidorAsync();
        var id = await LeerOkAsync(e, 2026, 10, 27, 15m);

        var r = await Corregir(e).EjecutarAsync(new CorregirLecturaSolicitud(id, new DateOnly(2026, 10, 27), 16m, null, "  "));

        Assert.False(r.EsExitoso);
        Assert.Equal(15m, e.Repo.Mediciones.Single().Lectura);
    }

    [Fact]
    public async Task Corregir_ConReciboEmitido_PideAnularloPrimero()
    {
        var e = new Escenario();
        await e.InstalarMedidorAsync();
        var id = await LeerOkAsync(e, 2026, 10, 27, 15m);
        e.Repo.RecibosEmitidos[id] = new ReciboDeLectura(1, "CB-REC-2026-000001");

        var r = await Corregir(e).EjecutarAsync(new CorregirLecturaSolicitud(id, new DateOnly(2026, 10, 27), 16m, null, Motivo));

        Assert.False(r.EsExitoso);
        Assert.Contains("CB-REC-2026-000001", r.Errores[0]);
        Assert.Contains("Anule primero", r.Errores[0]);
    }

    [Fact]
    public async Task Corregir_LecturaQueNoEsLaUltima_Falla()
    {
        var e = new Escenario();
        await e.InstalarMedidorAsync();
        var setiembre = await LeerOkAsync(e, 2026, 9, 27, 100m);
        await LeerOkAsync(e, 2026, 10, 27, 120m);

        var r = await Corregir(e).EjecutarAsync(new CorregirLecturaSolicitud(setiembre, new DateOnly(2026, 9, 27), 105m, null, Motivo));

        Assert.False(r.EsExitoso);
        Assert.Contains("ultima lectura", r.Errores[0]);
    }

    [Fact]
    public async Task Corregir_EnPeriodoCerrado_Falla()
    {
        var e = new Escenario();
        await e.InstalarMedidorAsync();
        var id = await LeerOkAsync(e, 2026, 10, 27, 15m);
        CerrarPeriodo(e, 2026, 10);

        var r = await Corregir(e).EjecutarAsync(new CorregirLecturaSolicitud(id, new DateOnly(2026, 10, 27), 16m, null, Motivo));

        Assert.False(r.EsExitoso);
        Assert.Contains("cerrado", r.Errores[0]);
    }

    [Fact]
    public async Task Corregir_ConLecturaMenorQueLaAnterior_Falla()
    {
        var e = new Escenario();
        await e.InstalarMedidorAsync();
        await LeerOkAsync(e, 2026, 9, 27, 100m);
        var id = await LeerOkAsync(e, 2026, 10, 27, 120m);

        var r = await Corregir(e).EjecutarAsync(new CorregirLecturaSolicitud(id, new DateOnly(2026, 10, 27), 90m, null, Motivo));

        Assert.False(r.EsExitoso);
        Assert.Equal(120m, e.Repo.Mediciones.Single(x => x.Id == id).Lectura);
    }

    [Fact]
    public async Task Corregir_ConMedidorYaRetirado_NoPuedePasarDeSuLecturaFinal()
    {
        var e = new Escenario();
        var medidor = await e.InstalarMedidorAsync("M-1");
        var id = await LeerOkAsync(e, 2026, 9, 27, 100m);
        Assert.True((await e.Retirar.EjecutarAsync(new RetirarMedidorSolicitud(medidor, new DateOnly(2026, 10, 10), 110m))).EsExitoso);
        await e.InstalarMedidorAsync("M-2", 0m, new DateOnly(2026, 10, 10));

        var r = await Corregir(e).EjecutarAsync(new CorregirLecturaSolicitud(id, new DateOnly(2026, 9, 27), 112m, null, Motivo));

        Assert.False(r.EsExitoso);
        Assert.Contains("lectura final", r.Errores[0]);

        var ok = await Corregir(e).EjecutarAsync(new CorregirLecturaSolicitud(id, new DateOnly(2026, 9, 27), 108m, null, Motivo));
        Assert.True(ok.EsExitoso, string.Join(" ", ok.Errores));
    }

    [Fact]
    public async Task Anular_DejaElPeriodoLibreParaRegistrarDeNuevo()
    {
        var e = new Escenario();
        await e.InstalarMedidorAsync();
        await LeerOkAsync(e, 2026, 9, 27, 100m);
        var id = await LeerOkAsync(e, 2026, 10, 27, 500m);

        var r = await Anular(e).EjecutarAsync(new AnularLecturaSolicitud(id, "Se registro en el servicio equivocado"));

        Assert.True(r.EsExitoso);
        var anulada = e.Repo.Mediciones.Single(x => x.Id == id);
        Assert.Equal(EstadoMedicion.Anulada, anulada.Estado);
        Assert.Equal("Admin Cuatro Bocas", anulada.AnuladaPorNombre);

        // La anulada ya no cuenta: el periodo vuelve a estar sin lectura y se puede registrar.
        Assert.False((await e.LecturasPeriodo.EjecutarAsync(2026, 10)).Valor!.Filas.Single().TieneLectura);
        var nueva = await e.LeerAsync(2026, 10, 27, 112m);
        Assert.True(nueva.EsExitoso, string.Join(" ", nueva.Errores));
        Assert.Equal(12m, nueva.Valor!.Consumo);
    }

    [Fact]
    public async Task Anular_ConReciboEmitido_Falla()
    {
        var e = new Escenario();
        await e.InstalarMedidorAsync();
        var id = await LeerOkAsync(e, 2026, 10, 27, 15m);
        e.Repo.RecibosEmitidos[id] = new ReciboDeLectura(4, "CB-REC-2026-000004");

        var r = await Anular(e).EjecutarAsync(new AnularLecturaSolicitud(id, "Se registro en el servicio equivocado"));

        Assert.False(r.EsExitoso);
        Assert.True(e.Repo.Mediciones.Single().EstaVigente);
    }

    [Fact]
    public async Task Anular_DosVeces_LaSegundaFalla()
    {
        var e = new Escenario();
        await e.InstalarMedidorAsync();
        var id = await LeerOkAsync(e, 2026, 10, 27, 15m);
        Assert.True((await Anular(e).EjecutarAsync(new AnularLecturaSolicitud(id, "Se registro en el servicio equivocado"))).EsExitoso);

        var r = await Anular(e).EjecutarAsync(new AnularLecturaSolicitud(id, "Se registro en el servicio equivocado"));

        Assert.False(r.EsExitoso);
    }

    [Fact]
    public async Task Obtener_ExplicaPorQueNoSePuedeModificar()
    {
        var e = new Escenario();
        await e.InstalarMedidorAsync();
        var setiembre = await LeerOkAsync(e, 2026, 9, 27, 100m);
        var octubre = await LeerOkAsync(e, 2026, 10, 27, 120m);
        var obtener = new ObtenerLecturaCasoUso(e.Repo);

        var vieja = (await obtener.EjecutarAsync(setiembre)).Valor!;
        Assert.False(vieja.PuedeModificarse);
        Assert.Contains("posterior", vieja.RazonNoModificable);

        var ultima = (await obtener.EjecutarAsync(octubre)).Valor!;
        Assert.True(ultima.PuedeModificarse);
        Assert.Equal("CB-AB-000001", ultima.AbonadoCodigo);
        Assert.Equal("M-1", ultima.NumeroSerieMedidor);
        Assert.Equal(20m, ultima.Consumo);
        Assert.Equal("Ana Operadora", ultima.RegistradaPorNombre);

        Assert.False((await obtener.EjecutarAsync(999)).EsExitoso);
    }
}
