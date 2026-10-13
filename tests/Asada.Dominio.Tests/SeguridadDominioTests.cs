using Asada.Dominio.Entidades;
using Asada.Dominio.Enums;
using Asada.Dominio.Reglas;

namespace Asada.Dominio.Tests;

public class ReglasMotivoTests
{
    [Fact]
    public void MotivoNulo_NoEsValido() => Assert.NotNull(ReglasMotivo.Validar(null));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("error")]
    public void MotivoVacioOCorto_NoEsValido(string? motivo) => Assert.NotNull(ReglasMotivo.Validar(motivo));

    [Fact]
    public void MotivoDemasiadoLargo_NoEsValido() => Assert.NotNull(ReglasMotivo.Validar(new string('x', 401)));

    [Fact]
    public void MotivoQueExplica_EsValido() => Assert.Null(ReglasMotivo.Validar("  Lectura digitada en el servicio equivocado  "));

    [Theory]
    [InlineData(9.99, false)]
    [InlineData(10, true)]
    [InlineData(50, true)]
    [InlineData(1000, true)]
    [InlineData(1000.01, false)]
    public void UmbralDeConsumoAnormal_TieneLimites(double porcentaje, bool valido)
        => Assert.Equal(valido, ReglasConsumoAnormal.EsUmbralValido((decimal)porcentaje));
}

public class AnulacionesYCierreTests
{
    private static readonly DateTime Ahora = new(2026, 11, 2, 15, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Medicion_Anular_DejaConstanciaYNoSePuedeAnularDosVeces()
    {
        var m = new Medicion { Lectura = 120m };
        m.Anular("  Se registro en el servicio equivocado ", 3, "Admin", Ahora);

        Assert.Equal(EstadoMedicion.Anulada, m.Estado);
        Assert.False(m.EstaVigente);
        Assert.Equal("Se registro en el servicio equivocado", m.MotivoAnulacion);
        Assert.Equal("Admin", m.AnuladaPorNombre);
        Assert.Equal(3, m.AnuladaPorUsuarioId);
        Assert.Equal(Ahora, m.FechaAnulacion);
        Assert.Throws<InvalidOperationException>(() => m.Anular("Otra vez por si acaso", 3, "Admin", Ahora));
    }

    [Fact]
    public void Medicion_AnularSinMotivo_Lanza()
        => Assert.Throws<InvalidOperationException>(() => new Medicion().Anular("mal", 1, "A", Ahora));

    [Fact]
    public void Medicion_Corregir_ReemplazaValoresYMotivosDeRevision()
    {
        var m = new Medicion { Lectura = 120m, Consumo = 20m, RequiereRevision = true, MotivosRevision = "x" };
        m.Corregir(new DateOnly(2026, 10, 27), 125m, 25m, [], "obs", "El lector anoto mal un digito", 3, "Admin", Ahora);

        Assert.Equal(125m, m.Lectura);
        Assert.Equal(25m, m.Consumo);
        Assert.False(m.RequiereRevision);
        Assert.Null(m.MotivosRevision);
        Assert.Equal("El lector anoto mal un digito", m.MotivoCorreccion);
        Assert.Equal("Admin", m.CorregidaPorNombre);
        Assert.Equal(Ahora, m.FechaUltimaCorreccion);
    }

    [Fact]
    public void Medicion_CorregirAnulada_Lanza()
    {
        var m = new Medicion();
        m.Anular("Lectura de otro servicio", 1, "A", Ahora);
        Assert.Throws<InvalidOperationException>(() =>
            m.Corregir(new DateOnly(2026, 10, 27), 1m, 1m, [], null, "El lector anoto mal", 1, "A", Ahora));
    }

    [Fact]
    public void Recibo_Anular_CambiaEstadoYGuardaMotivo()
    {
        var r = NuevoRecibo();
        r.Anular("Consumo mal calculado por lectura erronea", 3, "Admin", Ahora);

        Assert.Equal(EstadoRecibo.Anulado, r.Estado);
        Assert.Equal("Consumo mal calculado por lectura erronea", r.MotivoAnulacion);
        Assert.Equal("Admin", r.AnuladoPorNombre);
        Assert.Throws<InvalidOperationException>(() => r.Anular("Consumo mal calculado otra vez", 3, "Admin", Ahora));
    }

    [Fact]
    public void Periodo_CerrarYReabrir()
    {
        var p = new Periodo { Anio = 2026, Mes = 10 };
        Assert.False(p.EstaCerrado);
        Assert.Throws<InvalidOperationException>(() => p.Reabrir("No estaba cerrado aun", 1, "A", Ahora));

        p.Cerrar(3, "Admin", Ahora);
        Assert.True(p.EstaCerrado);
        Assert.Equal("Admin", p.CerradoPorNombre);
        Assert.Throws<InvalidOperationException>(() => p.Cerrar(3, "Admin", Ahora));
        Assert.Throws<InvalidOperationException>(() => p.Reabrir("corto", 3, "Admin", Ahora));

        p.Reabrir("Hay que anular un recibo mal emitido", 3, "Admin", Ahora);
        Assert.False(p.EstaCerrado);
        Assert.Equal("Hay que anular un recibo mal emitido", p.MotivoReapertura);
        Assert.Equal("Admin", p.ReabiertoPorNombre);
    }

    [Fact]
    public void Organizacion_UmbralPorDefecto_Es50()
        => Assert.Equal(50m, new Organizacion { Codigo = "CB", Nombre = "X" }.PorcentajeConsumoAnormal);

    private static Recibo NuevoRecibo() => new()
    {
        AbonadoCodigo = "CB-AB-000001", AbonadoNombre = "Maria", PropiedadDireccion = "Casa",
        CategoriaNombre = "Domiciliar", NumeroSerieMedidor = "M-1", Total = 5000m,
    };
}
