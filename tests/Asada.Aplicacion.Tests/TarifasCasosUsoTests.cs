using Asada.Aplicacion.Tarifas;
using Asada.Aplicacion.Tarifas.Dtos;
using Asada.Dominio.Enums;
using Asada.Dominio.Reglas;
using Xunit;

namespace Asada.Aplicacion.Tests;

public class TarifasCasosUsoTests
{
    private static readonly DateOnly Hoy = new(2026, 10, 12);
    private readonly RepositorioTarifasEnMemoria repo = new();
    private readonly ProveedorOrganizacionFalso org = new(1);
    private readonly ProveedorFechaFalso fecha = new(Hoy);

    private static readonly TramoTarifa[] TramosValidos = [new(15m, 300m), new(null, 500m)];

    private Task<Common.Resultado<int>> CrearTarifaAsync(int categoriaId, DateOnly desde, TramoTarifa[]? tramos = null,
        ModoHidrante modo = ModoHidrante.NoAplica, decimal hidrante = 0m)
        => new CrearTarifaCasoUso(repo, org).EjecutarAsync(new CrearTarifaSolicitud(
            categoriaId, desde, 2000m, modo, hidrante, null, tramos ?? TramosValidos));

    [Fact]
    public async Task CrearCategoria_GuardaConLaOrganizacionDelUsuario()
    {
        var r = await new CrearCategoriaCasoUso(repo, org).EjecutarAsync(new CrearCategoriaSolicitud("  Domiciliar ", null));
        Assert.True(r.EsExitoso);
        var c = Assert.Single(repo.Categorias);
        Assert.Equal("Domiciliar", c.Nombre);
        Assert.Equal(1, c.OrganizacionId);
    }

    [Fact]
    public async Task CrearCategoria_NombreDuplicadoSinImportarMayusculas_Falla()
    {
        repo.NuevaCategoria("Domiciliar");
        var r = await new CrearCategoriaCasoUso(repo, org).EjecutarAsync(new CrearCategoriaSolicitud("DOMICILIAR", null));
        Assert.False(r.EsExitoso);
    }

    [Fact]
    public async Task CrearCategoria_SinNombre_Falla()
        => Assert.False((await new CrearCategoriaCasoUso(repo, org).EjecutarAsync(new CrearCategoriaSolicitud(" ", null))).EsExitoso);

    [Fact]
    public async Task CrearCategoria_Superadministrador_Falla()
        => Assert.False((await new CrearCategoriaCasoUso(repo, new ProveedorOrganizacionFalso(null))
            .EjecutarAsync(new CrearCategoriaSolicitud("Domiciliar", null))).EsExitoso);

    [Fact]
    public async Task CrearTarifa_GuardaLosTramosEnOrden()
    {
        var c = repo.NuevaCategoria("Domiciliar");
        var r = await CrearTarifaAsync(c.Id, new DateOnly(2026, 1, 1));
        Assert.True(r.EsExitoso);
        var t = Assert.Single(repo.Tarifas);
        Assert.Equal(2, t.Bloques.Count);
        Assert.Equal(1, t.Bloques[0].Orden);
        Assert.Equal(15m, t.Bloques[0].LimiteSuperior);
        Assert.Null(t.Bloques[1].LimiteSuperior);
    }

    [Fact]
    public async Task CrearTarifa_TramosInvalidos_Falla()
    {
        var c = repo.NuevaCategoria("Domiciliar");
        var r = await CrearTarifaAsync(c.Id, new DateOnly(2026, 1, 1), [new(15m, 300m)]);
        Assert.False(r.EsExitoso);
        Assert.Empty(repo.Tarifas);
    }

    [Fact]
    public async Task CrearTarifa_CategoriaInactiva_Falla()
    {
        var c = repo.NuevaCategoria("Vieja", EstadoRegistro.Inactivo);
        Assert.False((await CrearTarifaAsync(c.Id, new DateOnly(2026, 1, 1))).EsExitoso);
    }

    [Fact]
    public async Task CrearTarifa_NuevaVersionDebeSerPosteriorALaUltima()
    {
        var c = repo.NuevaCategoria("Domiciliar");
        await CrearTarifaAsync(c.Id, new DateOnly(2026, 6, 1));
        var r = await CrearTarifaAsync(c.Id, new DateOnly(2026, 6, 1));
        Assert.False(r.EsExitoso);
        Assert.Contains("fecha posterior", r.Errores[0]);
        Assert.True((await CrearTarifaAsync(c.Id, new DateOnly(2026, 11, 1))).EsExitoso);
    }

    [Fact]
    public async Task CrearTarifa_VersionesDeOtraCategoriaNoInterfieren()
    {
        var a = repo.NuevaCategoria("Domiciliar");
        var b = repo.NuevaCategoria("Empresarial");
        await CrearTarifaAsync(a.Id, new DateOnly(2026, 6, 1));
        Assert.True((await CrearTarifaAsync(b.Id, new DateOnly(2026, 1, 1))).EsExitoso);
    }

    [Fact]
    public async Task CrearTarifa_NoPuedeRegirEnUnMesYaFacturado()
    {
        var c = repo.NuevaCategoria("Domiciliar");
        repo.UltimoPeriodoFacturado = (2026, 10);
        var r = await CrearTarifaAsync(c.Id, new DateOnly(2026, 10, 20));
        Assert.False(r.EsExitoso);
        Assert.Contains("01/11/2026", r.Errores[0]);
        Assert.True((await CrearTarifaAsync(c.Id, new DateOnly(2026, 11, 1))).EsExitoso);
    }

    [Fact]
    public async Task CrearTarifa_SinHidrantes_GuardaValorCero()
    {
        var c = repo.NuevaCategoria("Domiciliar");
        await CrearTarifaAsync(c.Id, new DateOnly(2026, 1, 1), modo: ModoHidrante.PorMetroCubico, hidrante: 12m);
        Assert.Equal(12m, repo.Tarifas[0].ValorHidrante);
    }

    [Fact]
    public async Task Panel_MarcaLaVersionQueRigeHoyYLasFuturas()
    {
        var c = repo.NuevaCategoria("Domiciliar");
        await CrearTarifaAsync(c.Id, new DateOnly(2025, 1, 1));
        await CrearTarifaAsync(c.Id, new DateOnly(2026, 6, 1));
        await CrearTarifaAsync(c.Id, new DateOnly(2027, 1, 1));
        repo.NuevoServicio(c.Id);
        repo.NuevoServicio();

        var panel = await new ObtenerPanelTarifasCasoUso(repo, fecha).EjecutarAsync();
        var fila = Assert.Single(panel.Categorias);
        Assert.Equal(3, fila.Versiones.Count);
        Assert.Equal(new DateOnly(2027, 1, 1), fila.Versiones[0].VigenteDesde);
        Assert.True(fila.Versiones[0].EsFutura);
        Assert.Equal(new DateOnly(2026, 6, 1), fila.VersionVigente!.VigenteDesde);
        Assert.Equal(1, fila.Categoria.CantidadServicios);
        Assert.Equal(1, panel.ServiciosSinCategoria);
    }

    [Fact]
    public async Task ObtenerTarifa_DescribeLosTramos()
    {
        var c = repo.NuevaCategoria("Domiciliar");
        await CrearTarifaAsync(c.Id, new DateOnly(2026, 1, 1));
        var r = await new ObtenerTarifaCasoUso(repo, fecha).EjecutarAsync(1);
        Assert.True(r.EsExitoso);
        Assert.Equal("Domiciliar", r.Valor!.CategoriaNombre);
        Assert.Equal("0 a 15 m³", r.Valor.Tramos[0].Descripcion);
        Assert.Equal("mas de 15 m³", r.Valor.Tramos[1].Descripcion);
        Assert.Equal(15m, r.Valor.Tramos[1].Desde);
        Assert.True(r.Valor.RigeHoy);
    }

    [Fact]
    public async Task Simular_CalculaSinGuardar()
    {
        var c = repo.NuevaCategoria("Domiciliar");
        await CrearTarifaAsync(c.Id, new DateOnly(2026, 1, 1));
        var r = await new SimularReciboCasoUso(repo).EjecutarAsync(1, 20m);
        Assert.True(r.EsExitoso);
        Assert.Equal(2000m + 4500m + 2500m, r.Valor!.Total);
    }

    [Fact]
    public async Task AsignarCategoria_CambiaLaCategoriaDelServicio()
    {
        var c = repo.NuevaCategoria("Domiciliar");
        var s = repo.NuevoServicio();
        var r = await new AsignarCategoriaServicioCasoUso(repo).EjecutarAsync(new AsignarCategoriaSolicitud(s.Id, c.Id));
        Assert.True(r.EsExitoso);
        Assert.Equal(c.Id, s.CategoriaId);
        Assert.Equal(1, repo.Guardados);
    }

    [Fact]
    public async Task AsignarCategoria_Inactiva_Falla()
    {
        var c = repo.NuevaCategoria("Vieja", EstadoRegistro.Inactivo);
        var s = repo.NuevoServicio();
        Assert.False((await new AsignarCategoriaServicioCasoUso(repo).EjecutarAsync(new AsignarCategoriaSolicitud(s.Id, c.Id))).EsExitoso);
        Assert.Null(s.CategoriaId);
    }

    [Fact]
    public async Task AsignarCategoria_Inexistente_Falla()
    {
        var s = repo.NuevoServicio();
        Assert.False((await new AsignarCategoriaServicioCasoUso(repo).EjecutarAsync(new AsignarCategoriaSolicitud(s.Id, 99))).EsExitoso);
    }

    [Fact]
    public async Task ObtenerCategoriaServicio_OcultaCategoriasInactivasSalvoLaActual()
    {
        var activa = repo.NuevaCategoria("Domiciliar");
        var vieja = repo.NuevaCategoria("Vieja", EstadoRegistro.Inactivo);
        repo.NuevaCategoria("Otra inactiva", EstadoRegistro.Inactivo);
        var s = repo.NuevoServicio(vieja.Id);

        var r = await new ObtenerCategoriaServicioCasoUso(repo).EjecutarAsync(s.Id);
        Assert.True(r.EsExitoso);
        Assert.Equal(2, r.Valor!.CategoriasDisponibles.Count);
        Assert.Equal(vieja.Id, r.Valor.CategoriaId);
        Assert.Contains(r.Valor.CategoriasDisponibles, c => c.Id == activa.Id);
    }
}
