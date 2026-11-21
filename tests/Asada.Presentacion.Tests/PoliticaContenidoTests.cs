using System.Text;
using Asada.Presentacion.Seguridad;

namespace Asada.Presentacion.Tests;

public class PoliticaContenidoTests
{
    [Theory]
    [InlineData("aplicar", PoliticaContenido.Modo.Aplicar)]
    [InlineData(" Aplicar ", PoliticaContenido.Modo.Aplicar)]
    [InlineData("informe", PoliticaContenido.Modo.Informe)]
    [InlineData("", PoliticaContenido.Modo.Informe)]
    [InlineData(null, PoliticaContenido.Modo.Informe)]
    [InlineData("otro", PoliticaContenido.Modo.Informe)]
    public void LeerModo_PorDefectoEsInforme(string? valor, PoliticaContenido.Modo esperado)
        => Assert.Equal(esperado, PoliticaContenido.LeerModo(valor));

    [Fact]
    public void NombreCabecera_SegunElModo()
    {
        Assert.Equal("Content-Security-Policy", PoliticaContenido.NombreCabecera(PoliticaContenido.Modo.Aplicar));
        Assert.Equal("Content-Security-Policy-Report-Only", PoliticaContenido.NombreCabecera(PoliticaContenido.Modo.Informe));
    }

    [Fact]
    public void Politica_NoPermiteCodigoEnElHtml()
    {
        Assert.DoesNotContain("unsafe-inline", PoliticaContenido.Politica);
        Assert.DoesNotContain("unsafe-eval", PoliticaContenido.Politica);
        Assert.Contains("script-src 'self'", PoliticaContenido.Politica);
        Assert.Contains("frame-ancestors 'none'", PoliticaContenido.Politica);
        Assert.Contains("report-uri " + PoliticaContenido.RutaAviso, PoliticaContenido.Politica);
    }

    [Theory]
    [InlineData("application/pdf", true)]
    [InlineData("application/PDF; charset=binary", true)]
    [InlineData("text/html; charset=utf-8", false)]
    [InlineData(null, false)]
    public void EsPdf(string? tipo, bool esperado) => Assert.Equal(esperado, PoliticaContenido.EsPdf(tipo));

    [Fact]
    public async Task LeerAviso_RecortaYQuitaSaltosDeLinea()
    {
        var texto = "{\"csp-report\":\r\n{}}" + new string('x', 10_000);
        using var cuerpo = new MemoryStream(Encoding.UTF8.GetBytes(texto));

        var aviso = await PoliticaContenido.LeerAvisoAsync(cuerpo, CancellationToken.None);

        Assert.Equal(PoliticaContenido.LargoMaximoAviso, aviso.Length);
        Assert.DoesNotContain('\n', aviso);
        Assert.DoesNotContain('\r', aviso);
        Assert.StartsWith("{\"csp-report\":  {}}", aviso);
    }
}
