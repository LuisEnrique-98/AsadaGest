using Asada.Aplicacion.Abonados;
using Asada.Aplicacion.Abonados.Dtos;
using Asada.Dominio.Enums;
using Xunit;

namespace Asada.Aplicacion.Tests;

public class CrearAbonadoCasoUsoTests
{
    private static CrearAbonadoSolicitud Solicitud(
        TipoIdentificacion tipo = TipoIdentificacion.CedulaFisica,
        string identificacion = "1-0234-0567", string nombre = "  Maria Rojas  ",
        string? correo = "maria@correo.cr")
        => new(tipo, identificacion, nombre, " 8888-1234 ", correo, null);

    [Fact]
    public async Task CreaElAbonado_ConIdentificacionNormalizadaYOrganizacionDelUsuario()
    {
        var repo = new RepositorioAbonadosEnMemoria();
        var casoUso = new CrearAbonadoCasoUso(repo, new ProveedorOrganizacionFalso(5));

        var resultado = await casoUso.EjecutarAsync(Solicitud());

        Assert.True(resultado.EsExitoso);
        var abonado = Assert.Single(repo.Abonados);
        Assert.Equal(5, abonado.OrganizacionId);
        Assert.Equal("102340567", abonado.Identificacion);
        Assert.Equal("Maria Rojas", abonado.Nombre);
        Assert.Equal("8888-1234", abonado.Telefono);
        Assert.Equal("CB-AB-000001", abonado.Codigo);
        Assert.Equal(abonado.Id, resultado.Valor);
    }

    [Fact]
    public async Task SinOrganizacion_ElSuperadministradorNoPuedeCrearAbonados()
    {
        var repo = new RepositorioAbonadosEnMemoria();
        var casoUso = new CrearAbonadoCasoUso(repo, new ProveedorOrganizacionFalso(null));

        var resultado = await casoUso.EjecutarAsync(Solicitud());

        Assert.False(resultado.EsExitoso);
        Assert.Empty(repo.Abonados);
    }

    [Fact]
    public async Task IdentificacionInvalida_FallaSinTocarElRepositorio()
    {
        var repo = new RepositorioAbonadosEnMemoria();
        var casoUso = new CrearAbonadoCasoUso(repo, new ProveedorOrganizacionFalso(1));

        var resultado = await casoUso.EjecutarAsync(Solicitud(identificacion: "123"));

        Assert.False(resultado.EsExitoso);
        Assert.Contains(resultado.Errores, e => e.Contains("9 digitos"));
        Assert.Empty(repo.Abonados);
    }

    [Fact]
    public async Task IdentificacionDuplicada_Falla_AunqueVengaConOtroFormato()
    {
        var repo = new RepositorioAbonadosEnMemoria();
        var casoUso = new CrearAbonadoCasoUso(repo, new ProveedorOrganizacionFalso(1));
        await casoUso.EjecutarAsync(Solicitud(identificacion: "1-0234-0567"));

        var segundo = await casoUso.EjecutarAsync(Solicitud(identificacion: "102340567", nombre: "Otra Persona"));

        Assert.False(segundo.EsExitoso);
        Assert.Contains(segundo.Errores, e => e.Contains("Ya existe"));
        Assert.Single(repo.Abonados);
    }

    [Fact]
    public async Task NombreVacioYCorreoInvalido_ReportanAmbosErrores()
    {
        var casoUso = new CrearAbonadoCasoUso(new RepositorioAbonadosEnMemoria(), new ProveedorOrganizacionFalso(1));

        var resultado = await casoUso.EjecutarAsync(Solicitud(nombre: "   ", correo: "no-es-correo"));

        Assert.False(resultado.EsExitoso);
        Assert.Equal(2, resultado.Errores.Count);
    }
}

public class PropiedadesYServiciosCasosUsoTests
{
    private static async Task<(RepositorioAbonadosEnMemoria repo, int abonadoId)> AbonadoExistenteAsync()
    {
        var repo = new RepositorioAbonadosEnMemoria();
        var r = await new CrearAbonadoCasoUso(repo, new ProveedorOrganizacionFalso(3)).EjecutarAsync(
            new CrearAbonadoSolicitud(TipoIdentificacion.CedulaFisica, "102340567", "Maria Rojas", null, null, null));
        return (repo, r.Valor);
    }

    [Fact]
    public async Task AgregarPropiedad_LaGuardaEnElAbonadoConSuOrganizacion()
    {
        var (repo, abonadoId) = await AbonadoExistenteAsync();
        var casoUso = new AgregarPropiedadCasoUso(repo);

        var resultado = await casoUso.EjecutarAsync(
            new AgregarPropiedadSolicitud(abonadoId, " Casa azul ", "San Jose", null, null, null));

        Assert.True(resultado.EsExitoso);
        var propiedad = Assert.Single(repo.Abonados[0].Propiedades);
        Assert.Equal("Casa azul", propiedad.Direccion);
        Assert.Equal(3, propiedad.OrganizacionId);
        Assert.Equal(1, repo.Guardados);
    }

    [Fact]
    public async Task AgregarPropiedad_ADeAbonadoInexistente_Falla()
    {
        var (repo, _) = await AbonadoExistenteAsync();

        var resultado = await new AgregarPropiedadCasoUso(repo).EjecutarAsync(
            new AgregarPropiedadSolicitud(999, "Casa", null, null, null, null));

        Assert.False(resultado.EsExitoso);
    }

    [Fact]
    public async Task AgregarServicio_CreaElServicioEnLaPropiedad()
    {
        var (repo, abonadoId) = await AbonadoExistenteAsync();
        var propiedadId = (await new AgregarPropiedadCasoUso(repo).EjecutarAsync(
            new AgregarPropiedadSolicitud(abonadoId, "Casa", null, null, null, null))).Valor;
        // en memoria los Id los asigna EF; aqui los simulamos
        repo.Abonados[0].Propiedades[0].Id = 10;

        var resultado = await new AgregarServicioCasoUso(repo).EjecutarAsync(
            new AgregarServicioSolicitud(abonadoId, 10, new DateOnly(2026, 2, 1), null, false, 0, 0m));

        Assert.True(resultado.EsExitoso);
        Assert.Single(repo.Abonados[0].Propiedades[0].Servicios);
        Assert.Equal(3, repo.Abonados[0].Propiedades[0].Servicios[0].OrganizacionId);
    }

    [Fact]
    public async Task AgregarServicio_ConPropiedadAjena_Falla()
    {
        var (repo, abonadoId) = await AbonadoExistenteAsync();

        var resultado = await new AgregarServicioCasoUso(repo).EjecutarAsync(
            new AgregarServicioSolicitud(abonadoId, 12345, new DateOnly(2026, 2, 1), null, false, 0, 0m));

        Assert.False(resultado.EsExitoso);
        Assert.Contains(resultado.Errores, e => e.Contains("propiedad"));
    }

    [Fact]
    public async Task AgregarServicio_ConDeudaPeroSinMarcarMoroso_Falla()
    {
        var (repo, abonadoId) = await AbonadoExistenteAsync();

        var resultado = await new AgregarServicioCasoUso(repo).EjecutarAsync(
            new AgregarServicioSolicitud(abonadoId, 1, new DateOnly(2026, 2, 1), null, false, 2, 10000m));

        Assert.False(resultado.EsExitoso);
        Assert.Contains(resultado.Errores, e => e.Contains("moroso"));
    }
}

public class BuscarYExpedienteCasosUsoTests
{
    [Theory]
    [InlineData(0, 500, 1, 100)]
    [InlineData(-4, 0, 1, 1)]
    [InlineData(3, 20, 3, 20)]
    public async Task Buscar_AjustaPaginaYTamanoALosLimites(int pagina, int tamano, int paginaEsperada, int tamanoEsperado)
    {
        var repo = new RepositorioAbonadosEnMemoria();

        await new BuscarAbonadosCasoUso(repo).EjecutarAsync("maria", pagina, tamano);

        Assert.Equal(paginaEsperada, repo.UltimaPaginaPedida);
        Assert.Equal(tamanoEsperado, repo.UltimoTamanoPedido);
    }

    [Fact]
    public async Task Expediente_DevuelveLaJerarquiaCompleta()
    {
        var repo = new RepositorioAbonadosEnMemoria();
        var id = (await new CrearAbonadoCasoUso(repo, new ProveedorOrganizacionFalso(1)).EjecutarAsync(
            new CrearAbonadoSolicitud(TipoIdentificacion.Dimex, "123456789012", "Juan Perez", null, null, null))).Valor;
        var propiedad = repo.Abonados[0].AgregarPropiedad("Casa 1");
        propiedad.AgregarServicio(new DateOnly(2026, 1, 1), esMoroso: true, mensualidadesPendientes: 2, montoPendiente: 9000m);

        var resultado = await new ObtenerExpedienteAbonadoCasoUso(repo).EjecutarAsync(id);

        Assert.True(resultado.EsExitoso);
        var expediente = resultado.Valor!;
        Assert.Equal("CB-AB-000001", expediente.Codigo);
        var p = Assert.Single(expediente.Propiedades);
        var s = Assert.Single(p.Servicios);
        Assert.True(s.EsMoroso);
        Assert.Equal(9000m, s.MontoPendiente);
    }

    [Fact]
    public async Task Expediente_DeAbonadoInexistente_Falla()
    {
        var resultado = await new ObtenerExpedienteAbonadoCasoUso(new RepositorioAbonadosEnMemoria()).EjecutarAsync(42);
        Assert.False(resultado.EsExitoso);
    }
}

public class EdicionDeAbonadosCasosUsoTests
{
    private static async Task<(RepositorioAbonadosEnMemoria repo, int id1, int id2)> DosAbonadosAsync()
    {
        var repo = new RepositorioAbonadosEnMemoria();
        var crear = new CrearAbonadoCasoUso(repo, new ProveedorOrganizacionFalso(1));
        var a = await crear.EjecutarAsync(new CrearAbonadoSolicitud(TipoIdentificacion.CedulaFisica, "102340567", "Maria Rojas", null, null, null));
        var b = await crear.EjecutarAsync(new CrearAbonadoSolicitud(TipoIdentificacion.CedulaFisica, "203450678", "Juan Perez", null, null, null));
        return (repo, a.Valor, b.Valor);
    }

    private static ActualizarAbonadoSolicitud Edicion(int id, string identificacion = "1-0234-0567", string nombre = " Maria Rojas Mora ")
        => new(id, TipoIdentificacion.CedulaFisica, identificacion, nombre, "8888-0000", "maria@correo.cr", " Centro ");

    [Fact]
    public async Task Actualizar_CambiaLosDatosYConservaElCodigo()
    {
        var (repo, id1, _) = await DosAbonadosAsync();

        var resultado = await new ActualizarAbonadoCasoUso(repo).EjecutarAsync(Edicion(id1));

        Assert.True(resultado.EsExitoso);
        var a = repo.Abonados[0];
        Assert.Equal("Maria Rojas Mora", a.Nombre);
        Assert.Equal("Centro", a.Direccion);
        Assert.Equal("CB-AB-000001", a.Codigo);
        Assert.Equal(1, repo.Guardados);
    }

    [Fact]
    public async Task Actualizar_ConsuPropiaIdentificacion_NoEsDuplicado()
    {
        var (repo, id1, _) = await DosAbonadosAsync();

        var resultado = await new ActualizarAbonadoCasoUso(repo).EjecutarAsync(Edicion(id1, "102340567"));

        Assert.True(resultado.EsExitoso);
    }

    [Fact]
    public async Task Actualizar_ConLaIdentificacionDeOtroAbonado_Falla()
    {
        var (repo, id1, _) = await DosAbonadosAsync();

        var resultado = await new ActualizarAbonadoCasoUso(repo).EjecutarAsync(Edicion(id1, "2-0345-0678"));

        Assert.False(resultado.EsExitoso);
        Assert.Contains(resultado.Errores, e => e.Contains("otro abonado"));
        Assert.Equal("102340567", repo.Abonados[0].Identificacion);
        Assert.Equal(0, repo.Guardados);
    }

    [Fact]
    public async Task Actualizar_ConDatosInvalidos_NoGuarda()
    {
        var (repo, id1, _) = await DosAbonadosAsync();

        var resultado = await new ActualizarAbonadoCasoUso(repo).EjecutarAsync(Edicion(id1, "123", "  "));

        Assert.False(resultado.EsExitoso);
        Assert.Equal(2, resultado.Errores.Count);
        Assert.Equal(0, repo.Guardados);
    }

    [Fact]
    public async Task Actualizar_AbonadoInexistente_Falla()
    {
        var (repo, _, _) = await DosAbonadosAsync();
        var resultado = await new ActualizarAbonadoCasoUso(repo).EjecutarAsync(Edicion(999));
        Assert.False(resultado.EsExitoso);
    }

    [Fact]
    public async Task CambiarEstado_DesactivaYReactiva_SinPerderDatos()
    {
        var (repo, id1, _) = await DosAbonadosAsync();
        var casoUso = new CambiarEstadoAbonadoCasoUso(repo);

        Assert.True((await casoUso.EjecutarAsync(new CambiarEstadoAbonadoSolicitud(id1, EstadoRegistro.Inactivo))).EsExitoso);
        Assert.Equal(EstadoRegistro.Inactivo, repo.Abonados[0].Estado);
        Assert.Single(repo.Abonados.Where(a => a.Id == id1));

        Assert.True((await casoUso.EjecutarAsync(new CambiarEstadoAbonadoSolicitud(id1, EstadoRegistro.Activo))).EsExitoso);
        Assert.Equal(EstadoRegistro.Activo, repo.Abonados[0].Estado);
        Assert.Equal(2, repo.Guardados);
    }

    [Fact]
    public async Task CambiarEstado_AlMismoEstado_NoGuardaNada()
    {
        var (repo, id1, _) = await DosAbonadosAsync();

        await new CambiarEstadoAbonadoCasoUso(repo).EjecutarAsync(new CambiarEstadoAbonadoSolicitud(id1, EstadoRegistro.Activo));

        Assert.Equal(0, repo.Guardados);
    }

    [Fact]
    public async Task CambiarEstado_ConValorInvalidoOAbonadoInexistente_Falla()
    {
        var (repo, id1, _) = await DosAbonadosAsync();
        var casoUso = new CambiarEstadoAbonadoCasoUso(repo);

        Assert.False((await casoUso.EjecutarAsync(new CambiarEstadoAbonadoSolicitud(id1, (EstadoRegistro)99))).EsExitoso);
        Assert.False((await casoUso.EjecutarAsync(new CambiarEstadoAbonadoSolicitud(999, EstadoRegistro.Inactivo))).EsExitoso);
    }
}
