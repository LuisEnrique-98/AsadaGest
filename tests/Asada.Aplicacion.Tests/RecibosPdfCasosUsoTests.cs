using Asada.Aplicacion.Recibos;
using Asada.Aplicacion.Recibos.Dtos;
using Asada.Dominio.Entidades;
using Asada.Dominio.Enums;
using Xunit;
using static Asada.Aplicacion.Tests.RepositorioRecibosEnMemoria;

namespace Asada.Aplicacion.Tests;

/// <summary>Generador falso: guarda lo que recibio y devuelve bytes fijos.</summary>
public sealed class GeneradorPdfFalso : IGeneradorPdfRecibos
{
    public List<IReadOnlyList<ReciboDto>> Llamadas { get; } = [];

    public byte[] Generar(IReadOnlyList<ReciboDto> recibos)
    {
        Llamadas.Add(recibos);
        return [0x25, 0x50, 0x44, 0x46]; // "%PDF"
    }
}

public class RecibosPdfCasosUsoTests
{
    private readonly RepositorioRecibosEnMemoria repo = new();
    private readonly ProveedorOrganizacionFalso org = new(1);
    private readonly GeneradorPdfFalso generador = new();

    public RecibosPdfCasosUsoTests()
    {
        repo.Periodos.Add(new Periodo { Id = 7, OrganizacionId = 1, Anio = 2026, Mes = 10 });
        repo.Periodos.Add(new Periodo { Id = 8, OrganizacionId = 1, Anio = 2026, Mes = 11 });
        repo.Tarifas.Add(TarifaEjemplo(1, categoriaId: 1, new DateOnly(2025, 1, 1)));
    }

    private DescargarPdfReciboCasoUso Individual => new(repo, generador);
    private DescargarPdfRecibosPeriodoCasoUso DelPeriodo => new(repo, org, generador);

    private async Task GenerarOctubreAsync(params ServicioAFacturar[] servicios)
    {
        repo.Servicios.AddRange(servicios);
        var r = await new GenerarRecibosPeriodoCasoUso(repo, org, new ProveedorFechaFalso(new DateOnly(2026, 10, 30)))
            .EjecutarAsync(new GenerarRecibosSolicitud(2026, 10, new DateOnly(2026, 11, 15)));
        Assert.True(r.EsExitoso);
    }

    [Fact]
    public async Task Individual_NoExiste_Falla()
    {
        var r = await Individual.EjecutarAsync(99);

        Assert.False(r.EsExitoso);
        Assert.Empty(generador.Llamadas);
    }

    [Fact]
    public async Task Individual_GeneraUnPdfConElNumeroDelRecibo()
    {
        await GenerarOctubreAsync(Listo(1, consumo: 20m));

        var r = await Individual.EjecutarAsync(1);

        Assert.True(r.EsExitoso);
        Assert.Equal("CB-REC-2026-000001.pdf", r.Valor!.NombreArchivo);
        Assert.Equal("application/pdf", r.Valor.TipoContenido);
        var dto = Assert.Single(Assert.Single(generador.Llamadas));
        Assert.Equal("CB-REC-2026-000001", dto.Numero);
        Assert.Equal("ASADA Cuatro Bocas", dto.OrganizacionNombre);
        Assert.Equal(10, dto.Mes);
        Assert.Equal(20m, dto.Consumo);
        Assert.NotEmpty(dto.Lineas);
    }

    [Fact]
    public async Task Individual_RecibosAnuladosTambienSePuedenDescargar()
    {
        await GenerarOctubreAsync(Listo(1));
        repo.Recibos[0].Estado = EstadoRecibo.Anulado;

        var r = await Individual.EjecutarAsync(1);

        Assert.True(r.EsExitoso);
        Assert.Equal(EstadoRecibo.Anulado, Assert.Single(Assert.Single(generador.Llamadas)).Estado);
    }

    [Fact]
    public async Task Periodo_SinRecibos_FallaSinGenerarPdf()
    {
        var r = await DelPeriodo.EjecutarAsync(2026, 10);

        Assert.False(r.EsExitoso);
        Assert.Contains("No hay recibos emitidos", r.Errores[0]);
        Assert.Empty(generador.Llamadas);
    }

    [Fact]
    public async Task Periodo_SinPeriodoRegistrado_Falla()
    {
        var r = await DelPeriodo.EjecutarAsync(2025, 3);

        Assert.False(r.EsExitoso);
        Assert.Empty(generador.Llamadas);
    }

    [Fact]
    public async Task Periodo_MesInvalido_Falla()
    {
        var r = await DelPeriodo.EjecutarAsync(2026, 13);

        Assert.False(r.EsExitoso);
        Assert.Contains("no es valido", r.Errores[0]);
    }

    [Fact]
    public async Task Periodo_UsuarioSinAsada_Falla()
    {
        await GenerarOctubreAsync(Listo(1));

        var r = await new DescargarPdfRecibosPeriodoCasoUso(repo, new ProveedorOrganizacionFalso(null), generador)
            .EjecutarAsync(2026, 10);

        Assert.False(r.EsExitoso);
        Assert.Empty(generador.Llamadas);
    }

    [Fact]
    public async Task Periodo_IncluyeTodosLosEmitidosOrdenadosPorNombre()
    {
        await GenerarOctubreAsync(
            Listo(3) with { AbonadoNombre = "Carlos Mora" },
            Listo(1) with { AbonadoNombre = "Ana Solis" },
            Listo(2) with { AbonadoNombre = "beatriz Rojas" });

        var r = await DelPeriodo.EjecutarAsync(2026, 10);

        Assert.True(r.EsExitoso);
        Assert.Equal("Recibos-CB-2026-10.pdf", r.Valor!.NombreArchivo);
        var dtos = Assert.Single(generador.Llamadas);
        Assert.Equal(["Ana Solis", "beatriz Rojas", "Carlos Mora"], dtos.Select(d => d.AbonadoNombre).ToList());
    }

    [Fact]
    public async Task Periodo_ExcluyeAnuladosYRecibosDeOtrosPeriodos()
    {
        await GenerarOctubreAsync(Listo(1), Listo(2), Listo(3));
        repo.Recibos.Single(x => x.ServicioId == 2).Estado = EstadoRecibo.Anulado;
        repo.Recibos.Add(new Recibo
        {
            Id = 50, OrganizacionId = 1, ServicioId = 4, PeriodoId = 8, Numero = "CB-REC-2026-000050",
            AbonadoCodigo = "CB-AB-000004", AbonadoNombre = "Abonado 4", PropiedadDireccion = "Casa",
            CategoriaNombre = "Domiciliar", NumeroSerieMedidor = "M-4",
        });

        var r = await DelPeriodo.EjecutarAsync(2026, 10);

        Assert.True(r.EsExitoso);
        var dtos = Assert.Single(generador.Llamadas);
        Assert.Equal([1, 3], dtos.Select(d => d.ServicioId).ToList());
    }

    [Fact]
    public async Task Periodo_SoloAnulados_Falla()
    {
        await GenerarOctubreAsync(Listo(1));
        repo.Recibos[0].Estado = EstadoRecibo.Anulado;

        var r = await DelPeriodo.EjecutarAsync(2026, 10);

        Assert.False(r.EsExitoso);
        Assert.Empty(generador.Llamadas);
    }
}
