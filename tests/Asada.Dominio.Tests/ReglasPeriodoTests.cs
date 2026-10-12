using Asada.Dominio.Enums;
using Asada.Dominio.Reglas;
using Xunit;

namespace Asada.Dominio.Tests;

public class ReglasPeriodoTests
{
    [Theory]
    [InlineData(2026, 10, 25, 31)]   // octubre: 25 al 31
    [InlineData(2026, 2, 22, 28)]    // febrero 2026 (no bisiesto)
    [InlineData(2028, 2, 23, 29)]    // febrero 2028 (bisiesto)
    [InlineData(2026, 4, 24, 30)]
    public void LaVentana_SonLosUltimosSieteDiasDelMes(int anio, int mes, int diaInicio, int diaFin)
    {
        Assert.Equal(new DateOnly(anio, mes, diaInicio), ReglasPeriodo.InicioVentana(anio, mes));
        Assert.Equal(new DateOnly(anio, mes, diaFin), ReglasPeriodo.FinVentana(anio, mes));
    }

    [Fact]
    public void EstadoVentana_DistingueNoInicia_Abierta_Y_Cerrada()
    {
        Assert.Equal(EstadoVentanaLectura.NoInicia, ReglasPeriodo.EstadoVentana(2026, 10, new DateOnly(2026, 10, 24)));
        Assert.Equal(EstadoVentanaLectura.Abierta, ReglasPeriodo.EstadoVentana(2026, 10, new DateOnly(2026, 10, 25)));
        Assert.Equal(EstadoVentanaLectura.Abierta, ReglasPeriodo.EstadoVentana(2026, 10, new DateOnly(2026, 10, 31)));
        Assert.Equal(EstadoVentanaLectura.Cerrada, ReglasPeriodo.EstadoVentana(2026, 10, new DateOnly(2026, 11, 1)));
    }

    [Theory]
    [InlineData(2026, 10, 25, true)]
    [InlineData(2026, 10, 31, true)]
    [InlineData(2026, 10, 24, false)]
    [InlineData(2026, 11, 1, false)]
    public void EstaEnVentana_SoloDentroDeLosSieteDias(int anio, int mes, int dia, bool esperado)
    {
        var fecha = dia > DateTime.DaysInMonth(anio, mes) ? new DateOnly(anio, mes, 1).AddMonths(1) : new DateOnly(anio, mes, dia);
        Assert.Equal(esperado, ReglasPeriodo.EstaEnVentana(anio, mes, fecha));
    }

    [Fact]
    public void AnteriorYSiguiente_CruzanElCambioDeAnio()
    {
        Assert.Equal((2025, 12), ReglasPeriodo.Anterior(2026, 1));
        Assert.Equal((2026, 9), ReglasPeriodo.Anterior(2026, 10));
        Assert.Equal((2027, 1), ReglasPeriodo.Siguiente(2026, 12));
    }

    [Fact]
    public void PeriodoPorDefecto_EsElMesActualSoloDesdeQueEmpiezaSuVentana()
    {
        Assert.Equal((2026, 9), ReglasPeriodo.PeriodoPorDefecto(new DateOnly(2026, 10, 9)));
        Assert.Equal((2026, 9), ReglasPeriodo.PeriodoPorDefecto(new DateOnly(2026, 10, 24)));
        Assert.Equal((2026, 10), ReglasPeriodo.PeriodoPorDefecto(new DateOnly(2026, 10, 25)));
        Assert.Equal((2025, 12), ReglasPeriodo.PeriodoPorDefecto(new DateOnly(2026, 1, 3)));
    }

    [Fact]
    public void Comparar_OrdenaPorAnioYLuegoPorMes()
    {
        Assert.True(ReglasPeriodo.Comparar(2026, 9, 2026, 10) < 0);
        Assert.True(ReglasPeriodo.Comparar(2027, 1, 2026, 12) > 0);
        Assert.Equal(0, ReglasPeriodo.Comparar(2026, 10, 2026, 10));
    }
}

public class ReglasConsumoAnormalTests
{
    [Fact]
    public void SuperaElPromedioEnMasDelUmbral_EsAnormal()
    {
        // Promedio de 10, 10, 10 = 10; umbral 50 % => anormal por encima de 15.
        Assert.True(ReglasConsumoAnormal.EsAnormal(16m, [10m, 10m, 10m]));
        Assert.False(ReglasConsumoAnormal.EsAnormal(15m, [10m, 10m, 10m]));
    }

    [Fact]
    public void ConMenosDeTresConsumosPrevios_NuncaEsAnormal()
        => Assert.False(ReglasConsumoAnormal.EsAnormal(500m, [10m, 10m]));

    [Fact]
    public void SoloUsaLosTresMasRecientes()
        => Assert.False(ReglasConsumoAnormal.EsAnormal(14m, [10m, 10m, 10m, 100m, 100m]));

    [Fact]
    public void PromedioEnCero_NoMarcaAnomalia()
        => Assert.False(ReglasConsumoAnormal.EsAnormal(20m, [0m, 0m, 0m]));

    [Fact]
    public void ElUmbralEsConfigurable()
    {
        Assert.False(ReglasConsumoAnormal.EsAnormal(18m, [10m, 10m, 10m], porcentajeUmbral: 100m));
        Assert.True(ReglasConsumoAnormal.EsAnormal(21m, [10m, 10m, 10m], porcentajeUmbral: 100m));
    }
}

public class MedidorTests
{
    private static Asada.Dominio.Entidades.Medidor Nuevo()
        => new() { NumeroSerie = "M-1", FechaInstalacion = new DateOnly(2026, 1, 10), LecturaInicial = 5m };

    [Fact]
    public void Retirar_CierraElMedidorConSuLecturaFinal()
    {
        var m = Nuevo();
        m.Retirar(new DateOnly(2026, 9, 1), 120m);

        Assert.Equal(EstadoMedidor.Retirado, m.Estado);
        Assert.Equal(120m, m.LecturaFinal);
        Assert.Equal(new DateOnly(2026, 9, 1), m.FechaRetiro);
    }

    [Fact]
    public void Retirar_DosVeces_Falla()
    {
        var m = Nuevo();
        m.Retirar(new DateOnly(2026, 9, 1), 120m);
        Assert.Throws<InvalidOperationException>(() => m.Retirar(new DateOnly(2026, 9, 2), 130m));
    }

    [Fact]
    public void Retirar_ConLecturaFinalMenorQueLaInicial_Falla()
        => Assert.Throws<InvalidOperationException>(() => Nuevo().Retirar(new DateOnly(2026, 9, 1), 4m));

    [Fact]
    public void Retirar_AntesDeLaInstalacion_Falla()
        => Assert.Throws<InvalidOperationException>(() => Nuevo().Retirar(new DateOnly(2026, 1, 9), 10m));
}
