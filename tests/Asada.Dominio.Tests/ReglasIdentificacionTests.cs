using Asada.Dominio.Enums;
using Asada.Dominio.Reglas;
using Xunit;

namespace Asada.Dominio.Tests;

public class ReglasIdentificacionTests
{
    [Theory]
    [InlineData(TipoIdentificacion.CedulaFisica, "1-0234-0567", "102340567")]
    [InlineData(TipoIdentificacion.CedulaFisica, " 1 0234 0567 ", "102340567")]
    [InlineData(TipoIdentificacion.CedulaJuridica, "3-101-123456", "3101123456")]
    [InlineData(TipoIdentificacion.Pasaporte, "ab-12345", "AB12345")]
    public void Normalizar_QuitaSeparadores(TipoIdentificacion tipo, string entrada, string esperado)
        => Assert.Equal(esperado, ReglasIdentificacion.Normalizar(tipo, entrada));

    [Theory]
    [InlineData(TipoIdentificacion.CedulaFisica, "1-0234-0567", true)]
    [InlineData(TipoIdentificacion.CedulaFisica, "1-0234-056", false)]   // 8 digitos
    [InlineData(TipoIdentificacion.CedulaFisica, "1-0234-056A", false)]  // letra
    [InlineData(TipoIdentificacion.CedulaJuridica, "3-101-123456", true)]
    [InlineData(TipoIdentificacion.CedulaJuridica, "102340567", false)]  // 9 digitos
    [InlineData(TipoIdentificacion.Dimex, "12345678901", true)]          // 11
    [InlineData(TipoIdentificacion.Dimex, "123456789012", true)]         // 12
    [InlineData(TipoIdentificacion.Dimex, "1234567890", false)]          // 10
    [InlineData(TipoIdentificacion.Pasaporte, "A1234567", true)]
    [InlineData(TipoIdentificacion.Pasaporte, "AB12", false)]
    [InlineData(TipoIdentificacion.CedulaFisica, "", false)]
    [InlineData(TipoIdentificacion.CedulaFisica, null, false)]
    public void EsValida_SegunElTipo(TipoIdentificacion tipo, string? valor, bool esperado)
        => Assert.Equal(esperado, ReglasIdentificacion.EsValida(tipo, valor));
}
