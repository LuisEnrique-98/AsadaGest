using System.Net;
using System.Security.Claims;
using Asada.Presentacion.Seguridad;
using Microsoft.AspNetCore.Http;

namespace Asada.Presentacion.Tests;

public class LimitesSolicitudesTests
{
    private static DefaultHttpContext Solicitud(string metodo, string ruta, string ip = "203.0.113.10", string? usuarioId = null)
    {
        var contexto = new DefaultHttpContext();
        contexto.Request.Method = metodo;
        contexto.Request.Path = ruta;
        contexto.Connection.RemoteIpAddress = IPAddress.Parse(ip);
        if (usuarioId is not null)
            contexto.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, usuarioId)], "Prueba"));
        return contexto;
    }

    [Theory]
    [InlineData("POST", "/cuenta/iniciar-sesion", LimitesSolicitudes.Tipo.Credenciales)]
    [InlineData("POST", "/cuenta/iniciar-sesion/", LimitesSolicitudes.Tipo.Credenciales)]
    [InlineData("POST", "/Cuenta/Iniciar-Sesion", LimitesSolicitudes.Tipo.Credenciales)]
    [InlineData("POST", "/cuenta/cambiar-contrasena", LimitesSolicitudes.Tipo.Credenciales)]
    [InlineData("GET", "/cuenta/iniciar-sesion", LimitesSolicitudes.Tipo.Ninguno)]
    [InlineData("GET", "/cuenta/cambiar-contrasena", LimitesSolicitudes.Tipo.Ninguno)]
    [InlineData("POST", "/cuenta/cerrar-sesion", LimitesSolicitudes.Tipo.Ninguno)]
    [InlineData("GET", "/recibos/pdf", LimitesSolicitudes.Tipo.PdfPeriodo)]
    [InlineData("GET", "/recibos/15/pdf", LimitesSolicitudes.Tipo.Ninguno)]
    [InlineData("POST", "/seguridad/csp-aviso", LimitesSolicitudes.Tipo.AvisoCsp)]
    [InlineData("GET", "/abonados", LimitesSolicitudes.Tipo.Ninguno)]
    [InlineData("GET", "/", LimitesSolicitudes.Tipo.Ninguno)]
    public void Clasificar_SoloLasRutasConLimite(string metodo, string ruta, LimitesSolicitudes.Tipo esperado)
        => Assert.Equal(esperado, LimitesSolicitudes.Clasificar(Solicitud(metodo, ruta).Request));

    [Fact]
    public void Ingreso_DiezPorMinutoPorIp()
    {
        using var limitador = LimitesSolicitudes.CrearLimitador(new OpcionesLimites());

        for (var i = 0; i < 10; i++)
            Assert.True(limitador.AttemptAcquire(Solicitud("POST", "/cuenta/iniciar-sesion")).IsAcquired);

        Assert.False(limitador.AttemptAcquire(Solicitud("POST", "/cuenta/iniciar-sesion")).IsAcquired);
        // El cambio de contrasena comparte el mismo tope.
        Assert.False(limitador.AttemptAcquire(Solicitud("POST", "/cuenta/cambiar-contrasena")).IsAcquired);
        // Otra IP no se ve afectada, y ver la pagina (GET) nunca tiene limite.
        Assert.True(limitador.AttemptAcquire(Solicitud("POST", "/cuenta/iniciar-sesion", "198.51.100.7")).IsAcquired);
        Assert.True(limitador.AttemptAcquire(Solicitud("GET", "/cuenta/iniciar-sesion")).IsAcquired);
    }

    [Fact]
    public void Ingreso_TopePorHora()
    {
        using var limitador = LimitesSolicitudes.CrearLimitador(new OpcionesLimites { IngresosPorMinuto = 1000, IngresosPorHora = 3 });

        for (var i = 0; i < 3; i++)
            Assert.True(limitador.AttemptAcquire(Solicitud("POST", "/cuenta/iniciar-sesion")).IsAcquired);

        Assert.False(limitador.AttemptAcquire(Solicitud("POST", "/cuenta/iniciar-sesion")).IsAcquired);
    }

    [Fact]
    public void Ingreso_IPv6_SeCuentaPorRed()
    {
        using var limitador = LimitesSolicitudes.CrearLimitador(new OpcionesLimites { IngresosPorMinuto = 2 });

        Assert.True(limitador.AttemptAcquire(Solicitud("POST", "/cuenta/iniciar-sesion", "2001:db8:1:2::10")).IsAcquired);
        Assert.True(limitador.AttemptAcquire(Solicitud("POST", "/cuenta/iniciar-sesion", "2001:db8:1:2::11")).IsAcquired);
        // Misma red /64, otra direccion: ya no pasa.
        Assert.False(limitador.AttemptAcquire(Solicitud("POST", "/cuenta/iniciar-sesion", "2001:db8:1:2:aaaa::1")).IsAcquired);
        // Otra red: pasa.
        Assert.True(limitador.AttemptAcquire(Solicitud("POST", "/cuenta/iniciar-sesion", "2001:db8:1:3::10")).IsAcquired);
    }

    [Theory]
    [InlineData("203.0.113.10", "203.0.113.10")]
    [InlineData("::ffff:203.0.113.10", "203.0.113.10")]
    [InlineData("2001:db8:1:2:3:4:5:6", "2001:db8:1:2::/64")]
    public void ClaveIp(string ip, string esperada)
        => Assert.Equal(esperada, LimitesSolicitudes.ClaveIp(Solicitud("POST", "/", ip)));

    [Fact]
    public void PdfPeriodo_UnoALaVezPorUsuario()
    {
        using var limitador = LimitesSolicitudes.CrearLimitador(new OpcionesLimites());

        var primero = limitador.AttemptAcquire(Solicitud("GET", "/recibos/pdf", usuarioId: "7"));
        Assert.True(primero.IsAcquired);
        // Mientras se genera el primero, el mismo usuario no puede pedir otro (aunque sea desde otra IP)...
        Assert.False(limitador.AttemptAcquire(Solicitud("GET", "/recibos/pdf", "198.51.100.7", usuarioId: "7")).IsAcquired);
        // ...pero otro usuario si.
        using (var otro = limitador.AttemptAcquire(Solicitud("GET", "/recibos/pdf", usuarioId: "8")))
            Assert.True(otro.IsAcquired);

        primero.Dispose();
        Assert.True(limitador.AttemptAcquire(Solicitud("GET", "/recibos/pdf", usuarioId: "7")).IsAcquired);
    }

    [Fact]
    public void PdfPeriodo_DiezPorMinutoPorUsuario()
    {
        using var limitador = LimitesSolicitudes.CrearLimitador(new OpcionesLimites());

        for (var i = 0; i < 10; i++)
            limitador.AttemptAcquire(Solicitud("GET", "/recibos/pdf", usuarioId: "7")).Dispose();

        Assert.False(limitador.AttemptAcquire(Solicitud("GET", "/recibos/pdf", usuarioId: "7")).IsAcquired);
        // El PDF de un solo recibo no tiene limite.
        Assert.True(limitador.AttemptAcquire(Solicitud("GET", "/recibos/15/pdf", usuarioId: "7")).IsAcquired);
    }

    [Theory]
    [InlineData(LimitesSolicitudes.Tipo.Credenciales)]
    [InlineData(LimitesSolicitudes.Tipo.PdfPeriodo)]
    public void PaginaRechazo_CumpleLaPoliticaDeContenido(LimitesSolicitudes.Tipo tipo)
    {
        var pagina = LimitesSolicitudes.PaginaRechazo(tipo);
        Assert.Contains("Demasiadas solicitudes", pagina);
        Assert.DoesNotContain("<script", pagina);
        Assert.DoesNotContain("style=", pagina);
    }
}
