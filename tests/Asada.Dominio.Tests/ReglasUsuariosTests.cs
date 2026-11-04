using Asada.Dominio.Reglas;

namespace Asada.Dominio.Tests;

/// <summary>Sprint 5b: politica de contrasenas y codigo de una ASADA nueva.</summary>
public class ReglasUsuariosTests
{
    [Theory]
    [InlineData("rioceleste26")]
    [InlineData("Cuatro Bocas 2026")]
    [InlineData("abcd-2345-efgh")]
    [InlineData("CuatroBocas#2026")]
    [InlineData("contraseña1a")]
    public void Contrasena_Valida(string contrasena)
        => Assert.Empty(ReglasContrasena.Validar(contrasena));

    [Theory]
    [InlineData(null, "Indique la contrasena.")]
    [InlineData("", "Indique la contrasena.")]
    [InlineData("abc12345", "La contrasena debe tener al menos 10 caracteres.")]
    [InlineData("abcdefghijk", "La contrasena debe incluir al menos un numero.")]
    [InlineData("12345678901", "La contrasena debe incluir al menos una letra.")]
    [InlineData(" espacio12345", "La contrasena no puede empezar ni terminar con espacios.")]
    public void Contrasena_Invalida(string? contrasena, string error)
        => Assert.Contains(error, ReglasContrasena.Validar(contrasena));

    [Fact]
    public void Contrasena_ExactamenteDiezCaracteresEsValida()
        => Assert.True(ReglasContrasena.EsValida("abcdefgh12"));

    [Fact]
    public void Contrasena_DemasiadoLarga()
        => Assert.Contains("La contrasena no puede superar 128 caracteres.", ReglasContrasena.Validar(new string('a', 128) + "1"));

    [Theory]
    [InlineData("maria.lopez2026", "maria.lopez@cb.test")]
    [InlineData("xMARIA.LOPEZ@CB.TEST1", "maria.lopez@cb.test")]
    public void Contrasena_NoPuedeContenerElCorreo(string contrasena, string correo)
        => Assert.Contains("La contrasena no puede contener su correo.", ReglasContrasena.Validar(contrasena, correo));

    [Fact]
    public void Contrasena_UsuarioDeCorreoMuyCortoNoCuenta()
        => Assert.True(ReglasContrasena.EsValida("ana-rio-2026", "ana@cb.test"));

    [Theory]
    [InlineData(" cb ", "CB")]
    [InlineData("lp", "LP")]
    [InlineData(null, "")]
    public void Codigo_SeNormaliza(string? codigo, string esperado)
        => Assert.Equal(esperado, ReglasOrganizacion.NormalizarCodigo(codigo));

    [Theory]
    [InlineData("CB")]
    [InlineData("SRAFA")]
    public void Codigo_Valido(string codigo) => Assert.Null(ReglasOrganizacion.ValidarCodigo(codigo));

    [Theory]
    [InlineData("")]
    [InlineData("C")]
    [InlineData("SRAFAE")]
    [InlineData("C1")]
    [InlineData("C B")]
    [InlineData("ÑA")]
    public void Codigo_Invalido(string codigo) => Assert.NotNull(ReglasOrganizacion.ValidarCodigo(codigo));
}
