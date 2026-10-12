using Asada.Dominio.Entidades;
using Asada.Dominio.Enums;
using Asada.Dominio.Reglas;

namespace Asada.Dominio.Tests;

public class ReglasTarifaTests
{
    private static IReadOnlyList<string> Validar(params TramoTarifa[] tramos)
        => ReglasTarifa.Validar(1000m, ModoHidrante.NoAplica, 0m, tramos);

    [Fact]
    public void TramosConsecutivosYUltimoAbierto_SonValidos()
        => Assert.Empty(Validar(new(15m, 300m), new(25m, 450m), new(null, 700m)));

    [Fact]
    public void SinTramos_EsInvalido() => Assert.NotEmpty(Validar());

    [Fact]
    public void UltimoTramoConLimite_EsInvalido()
        => Assert.Contains(Validar(new(15m, 300m), new(25m, 450m)), e => e.Contains("ultimo tramo"));

    [Fact]
    public void TramoIntermedioSinLimite_EsInvalido()
        => Assert.Contains(Validar(new(null, 300m), new(null, 450m)), e => e.Contains("Solo el ultimo"));

    [Fact]
    public void LimitesNoCrecientes_SonInvalidos()
        => Assert.Contains(Validar(new(25m, 300m), new(15m, 450m), new(null, 700m)), e => e.Contains("tramo 2"));

    [Fact]
    public void PrimerLimiteCero_EsInvalido()
        => Assert.Contains(Validar(new(0m, 300m), new(null, 450m)), e => e.Contains("primer tramo"));

    [Fact]
    public void PrecioNegativo_EsInvalido()
        => Assert.NotEmpty(Validar(new TramoTarifa(null, -1m)));

    [Fact]
    public void CargoFijoNegativo_EsInvalido()
        => Assert.NotEmpty(ReglasTarifa.Validar(-1m, ModoHidrante.NoAplica, 0m, [new(null, 100m)]));

    [Theory]
    [InlineData(ModoHidrante.NoAplica, 5, false)]
    [InlineData(ModoHidrante.NoAplica, 0, true)]
    [InlineData(ModoHidrante.PorMetroCubico, 0, false)]
    [InlineData(ModoHidrante.PorMetroCubico, 12.5, true)]
    [InlineData(ModoHidrante.MontoFijo, 300, true)]
    public void Hidrantes_ValorCoherenteConElModo(ModoHidrante modo, decimal valor, bool valido)
        => Assert.Equal(valido, ReglasTarifa.Validar(0m, modo, valor, [new(null, 100m)]).Count == 0);

    [Fact]
    public void MasDeDiezTramos_EsInvalido()
    {
        var tramos = Enumerable.Range(1, 11).Select(i => new TramoTarifa(i * 10m, 100m)).ToList();
        tramos[^1] = new TramoTarifa(null, 100m);
        Assert.Contains(ReglasTarifa.Validar(0m, ModoHidrante.NoAplica, 0m, tramos), e => e.Contains("maximo"));
    }

    [Fact]
    public void FechaReferencia_EsElUltimoDiaDelMes()
    {
        Assert.Equal(new DateOnly(2026, 2, 28), ReglasTarifa.FechaReferencia(2026, 2));
        Assert.Equal(new DateOnly(2028, 2, 29), ReglasTarifa.FechaReferencia(2028, 2));
        Assert.Equal(new DateOnly(2026, 10, 31), ReglasTarifa.FechaReferencia(2026, 10));
    }

    private static readonly List<Tarifa> Versiones =
    [
        new Tarifa { Id = 1, VigenteDesde = new DateOnly(2025, 1, 1) },
        new Tarifa { Id = 2, VigenteDesde = new DateOnly(2026, 10, 15) },
        new Tarifa { Id = 3, VigenteDesde = new DateOnly(2027, 1, 1) },
    ];

    [Fact]
    public void Vigente_EsLaUltimaQueYaEntroARegir()
        => Assert.Equal(1, ReglasTarifa.SeleccionarVigente(Versiones, new DateOnly(2026, 9, 30))!.Id);

    [Fact]
    public void Vigente_UnaVersionDeMitadDeMesRigeParaEsePeriodo()
        => Assert.Equal(2, ReglasTarifa.SeleccionarVigente(Versiones, ReglasTarifa.FechaReferencia(2026, 10))!.Id);

    [Fact]
    public void Vigente_NullSiNingunaHabiaEntradoARegir()
        => Assert.Null(ReglasTarifa.SeleccionarVigente(Versiones, new DateOnly(2024, 12, 31)));

    [Fact]
    public void NumeroDeRecibo_TieneElFormatoDelSrs()
        => Assert.Equal("CB-REC-2026-000125", ReglasRecibo.GenerarNumero("CB", 2026, 125));
}
