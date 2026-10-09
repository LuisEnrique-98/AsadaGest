using Asada.Dominio.Entidades;
using Asada.Dominio.Reglas;
using Xunit;

namespace Asada.Dominio.Tests;

public class CalculadoraConsumoTests
{
    private static Medidor Medidor(int id, string serie, decimal inicial = 0m, decimal? final = null)
    {
        var m = new Medidor { Id = id, NumeroSerie = serie, LecturaInicial = inicial, FechaInstalacion = new DateOnly(2025, 1, 1) };
        if (final is decimal f) m.Retirar(new DateOnly(2026, 6, 1), f);
        return m;
    }

    private static LecturaAnterior Anterior(decimal lectura, int medidorId)
        => new(lectura, medidorId, 2026, 8, new DateOnly(2026, 8, 28));

    [Fact]
    public void UnSoloMedidor_RestaLecturaActualMenosAnterior()
    {
        var r = CalculadoraConsumo.Calcular(Anterior(100m, 1), [Medidor(1, "A")], 118m);

        Assert.True(r.EsValido);
        Assert.Equal(18m, r.Consumo);
    }

    [Fact]
    public void PrimeraLectura_UsaLaLecturaInicialDelMedidorComoBase()
    {
        var r = CalculadoraConsumo.Calcular(null, [Medidor(1, "A", inicial: 5m)], 17m);

        Assert.True(r.EsValido);
        Assert.Equal(12m, r.Consumo);
    }

    [Fact]
    public void LecturaMenorQueLaAnterior_ConElMismoMedidor_EsInvalida()
    {
        var r = CalculadoraConsumo.Calcular(Anterior(100m, 1), [Medidor(1, "A")], 90m);

        Assert.False(r.EsValido);
        Assert.True(r.Error!.Contains("menor"));
    }

    [Fact]
    public void RB013_CambioDeMedidor_SumaElTramoSalienteYElEntrante()
    {
        // Ejemplo del SRS: el medidor viejo cierra en 320 y el nuevo empieza en 0.
        // Anterior = 300 (viejo). Tramo viejo = 320 - 300 = 20. Nuevo marca 8 => tramo nuevo = 8. Total 28.
        var viejo = Medidor(1, "VIEJO", inicial: 0m, final: 320m);
        var nuevo = Medidor(2, "NUEVO", inicial: 0m);

        var r = CalculadoraConsumo.Calcular(Anterior(300m, 1), [viejo, nuevo], 8m);

        Assert.True(r.EsValido);
        Assert.Equal(28m, r.Consumo);
    }

    [Fact]
    public void RB013_MedidorNuevoQueNoEmpiezaEnCero_RestaSuLecturaInicial()
    {
        var viejo = Medidor(1, "VIEJO", final: 320m);
        var nuevo = Medidor(2, "NUEVO", inicial: 4m);

        var r = CalculadoraConsumo.Calcular(Anterior(300m, 1), [viejo, nuevo], 10m);

        Assert.Equal(20m + 6m, r.Consumo);
    }

    [Fact]
    public void RB013_DosCambiosEntreLecturas_SumaLosTresTramos()
    {
        var a = Medidor(1, "A", final: 50m);
        var b = Medidor(2, "B", inicial: 0m, final: 7m);
        var c = Medidor(3, "C", inicial: 0m);

        // 50-40 = 10 ; 7-0 = 7 ; 3-0 = 3  => 20
        var r = CalculadoraConsumo.Calcular(Anterior(40m, 1), [a, b, c], 3m);

        Assert.True(r.EsValido);
        Assert.Equal(20m, r.Consumo);
    }

    [Fact]
    public void CambioDeMedidor_SinLecturaFinalDelSaliente_EsInvalido()
    {
        // El medidor saliente sigue "activo" (sin lectura final): no se puede cerrar el tramo.
        var sinCerrar = Medidor(1, "A");
        var nuevo = Medidor(2, "B");

        var r = CalculadoraConsumo.Calcular(Anterior(10m, 1), [sinCerrar, nuevo], 5m);

        Assert.False(r.EsValido);
        Assert.True(r.Error!.Contains("lectura final"));
    }

    [Fact]
    public void CadenaVaciaOQueNoEmpiezaEnElMedidorAnterior_EsInvalida()
    {
        Assert.False(CalculadoraConsumo.Calcular(null, [], 5m).EsValido);
        Assert.False(CalculadoraConsumo.Calcular(Anterior(10m, 99), [Medidor(1, "A")], 15m).EsValido);
    }

    [Fact]
    public void ConsumoCero_EsValido()
    {
        var r = CalculadoraConsumo.Calcular(Anterior(100m, 1), [Medidor(1, "A")], 100m);

        Assert.True(r.EsValido);
        Assert.Equal(0m, r.Consumo);
    }
}
