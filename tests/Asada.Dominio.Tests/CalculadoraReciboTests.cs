using Asada.Dominio.Entidades;
using Asada.Dominio.Enums;
using Asada.Dominio.Reglas;

namespace Asada.Dominio.Tests;

public class CalculadoraReciboTests
{
    /// <summary>Tarifa de ejemplo: cargo fijo 2000; 0-15 a 300, 15-25 a 450, mas de 25 a 700.</summary>
    private static Tarifa TarifaEjemplo(ModoHidrante modo = ModoHidrante.NoAplica, decimal hidrante = 0m) => new()
    {
        CargoFijo = 2000m,
        ModoHidrante = modo,
        ValorHidrante = hidrante,
        Bloques =
        [
            new TarifaBloque { Orden = 1, LimiteSuperior = 15m, PrecioPorMetroCubico = 300m },
            new TarifaBloque { Orden = 2, LimiteSuperior = 25m, PrecioPorMetroCubico = 450m },
            new TarifaBloque { Orden = 3, LimiteSuperior = null, PrecioPorMetroCubico = 700m },
        ],
    };

    private static DesgloseRecibo Calcular(Tarifa t, decimal consumo)
    {
        var r = CalculadoraRecibo.Calcular(t, consumo);
        Assert.True(r.EsValido, r.Error);
        return r.Desglose!;
    }

    [Fact]
    public void ConsumoCero_SoloCobraCargoFijo()
    {
        var d = Calcular(TarifaEjemplo(), 0m);
        var linea = Assert.Single(d.Lineas);
        Assert.Equal(TipoLineaRecibo.CargoFijo, linea.Tipo);
        Assert.Equal(2000m, d.Total);
    }

    [Fact]
    public void ConsumoDentroDelPrimerTramo()
    {
        var d = Calcular(TarifaEjemplo(), 10m);
        Assert.Equal(2000m + 10m * 300m, d.Total);
        Assert.Equal(2, d.Lineas.Count);
    }

    [Fact]
    public void ConsumoEnElLimiteExacto_NoPasaAlTramoSiguiente()
    {
        var d = Calcular(TarifaEjemplo(), 15m);
        Assert.Equal(2000m + 15m * 300m, d.Total);
        Assert.Single(d.Lineas, l => l.Tipo == TipoLineaRecibo.Consumo);
    }

    [Fact]
    public void ConsumoEscalonado_CadaMetroAlPrecioDeSuTramo()
    {
        // 15 x 300 + 5 x 450 (no 20 x 450).
        var d = Calcular(TarifaEjemplo(), 20m);
        Assert.Equal(2000m + 4500m + 2250m, d.Total);
        var consumo = d.Lineas.Where(l => l.Tipo == TipoLineaRecibo.Consumo).ToList();
        Assert.Equal(2, consumo.Count);
        Assert.Equal(15m, consumo[0].Cantidad);
        Assert.Equal(5m, consumo[1].Cantidad);
        Assert.Equal("Consumo 0 a 15 m³", consumo[0].Concepto);
        Assert.Equal("Consumo mas de 15 a 25 m³", consumo[1].Concepto);
    }

    [Fact]
    public void ConsumoEnElUltimoTramo_SinLimite()
    {
        // 15 x 300 + 10 x 450 + 15 x 700.
        var d = Calcular(TarifaEjemplo(), 40m);
        Assert.Equal(2000m + 4500m + 4500m + 10500m, d.Total);
        Assert.Equal("Consumo mas de 25 m³", d.Lineas.Last().Concepto);
        Assert.Equal(15m, d.Lineas.Last().Cantidad);
    }

    [Fact]
    public void ConsumoConDecimales_RedondeaCadaLineaYElTotalEsLaSuma()
    {
        // 15 x 300 = 4500; 0.333 x 450 = 149.85.
        var d = Calcular(TarifaEjemplo(), 15.333m);
        Assert.Equal(149.85m, d.Lineas.Last().Monto);
        Assert.Equal(d.Lineas.Sum(l => l.Monto), d.Total);
    }

    [Fact]
    public void RedondeoComercial_MedioCentimoSubeAlCentimoSiguiente()
    {
        Assert.Equal(0.13m, CalculadoraRecibo.Redondear(0.125m));
        Assert.Equal(10.01m, CalculadoraRecibo.Redondear(10.005m));
    }

    [Fact]
    public void HidrantesPorMetroCubico_SeCobranSobreTodoElConsumo()
    {
        var d = Calcular(TarifaEjemplo(ModoHidrante.PorMetroCubico, 12.5m), 20m);
        var hidrante = Assert.Single(d.Lineas, l => l.Tipo == TipoLineaRecibo.Hidrante);
        Assert.Equal(250m, hidrante.Monto);
        Assert.Equal(250m, d.MontoHidrante);
        Assert.Equal(2000m + 4500m + 2250m, d.SubtotalAgua);
        Assert.Equal(d.SubtotalAgua + 250m, d.Total);
    }

    [Fact]
    public void HidrantesPorMetroCubico_ConConsumoCero_NoAgreganLinea()
    {
        var d = Calcular(TarifaEjemplo(ModoHidrante.PorMetroCubico, 12.5m), 0m);
        Assert.Equal(0m, d.MontoHidrante);
        Assert.DoesNotContain(d.Lineas, l => l.Tipo == TipoLineaRecibo.Hidrante);
    }

    [Fact]
    public void HidrantesMontoFijo_SeCobranAunqueNoHayaConsumo()
    {
        var d = Calcular(TarifaEjemplo(ModoHidrante.MontoFijo, 300m), 0m);
        Assert.Equal(300m, d.MontoHidrante);
        Assert.Equal(2300m, d.Total);
    }

    [Fact]
    public void TramoUnico_TarifaPlanaPorMetro()
    {
        var t = new Tarifa
        {
            CargoFijo = 0m,
            Bloques = [new TarifaBloque { Orden = 1, LimiteSuperior = null, PrecioPorMetroCubico = 500m }],
        };
        var d = Calcular(t, 8m);
        var linea = Assert.Single(d.Lineas);
        Assert.Equal("Consumo mas de 0 m³", linea.Concepto);
        Assert.Equal(4000m, d.Total);
    }

    [Fact]
    public void BloquesDesordenados_SeUsanSegunSuOrden()
    {
        var t = TarifaEjemplo();
        t.Bloques.Reverse();
        Assert.Equal(2000m + 4500m + 2250m, Calcular(t, 20m).Total);
    }

    [Fact]
    public void ConsumoNegativo_Falla()
        => Assert.False(CalculadoraRecibo.Calcular(TarifaEjemplo(), -1m).EsValido);

    [Fact]
    public void TarifaInvalida_FallaSinLanzarExcepcion()
    {
        var t = TarifaEjemplo();
        t.Bloques.Clear();
        var r = CalculadoraRecibo.Calcular(t, 5m);
        Assert.False(r.EsValido);
        Assert.Contains("al menos un tramo", r.Error);
    }
}
