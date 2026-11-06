using Xunit;
using Asada.Aplicacion.Auditoria;
using Asada.Aplicacion.Organizaciones;
using Asada.Aplicacion.Usuarios;
using Asada.Dominio.Enums;

namespace Asada.Aplicacion.Tests;

/// <summary>Sprint 5b: gestion de usuarios de una ASADA y registro de ASADAs nuevas.</summary>
public class UsuariosCasosUsoTests
{
    private readonly GestorUsuariosEnMemoria gestor = new();
    private readonly RepositorioOrganizacionesEnMemoria organizaciones = new();
    private readonly GeneradorContrasenaFijo generador = new();

    // Cuatro Bocas (1): Ana es la Administradora que usa el sistema; Beto, Operador.
    private readonly GestorUsuariosEnMemoria.UsuarioFalso ana;
    private readonly GestorUsuariosEnMemoria.UsuarioFalso beto;
    // San Rafael (2): otra ASADA.
    private readonly GestorUsuariosEnMemoria.UsuarioFalso ximena;

    public UsuariosCasosUsoTests()
    {
        ana = gestor.Agregar(1, "Ana Admin", RolesUsuario.Administrador, correo: "ana@cb.test");
        beto = gestor.Agregar(1, "Beto Operador", RolesUsuario.Operador, correo: "beto@cb.test");
        ximena = gestor.Agregar(2, "Ximena Admin", RolesUsuario.Administrador, correo: "ximena@sr.test");
    }

    private static ProveedorOrganizacionFalso Asada(int id) => new(id);
    private static ProveedorOrganizacionFalso Superadmin() => new(null);
    private ProveedorUsuarioFalso ComoAna() => new(ana.Id, ana.Nombre);
    private static ProveedorUsuarioFalso ComoSuperadmin() => new(99, "Super");

    // ---- Alcance por organizacion ----

    [Fact]
    public async Task Listar_DevuelveSoloUsuariosDeSuAsada()
    {
        var r = await new ListarUsuariosCasoUso(gestor, organizaciones, Asada(1)).EjecutarAsync();

        Assert.True(r.EsExitoso);
        Assert.Equal("CB", r.Valor!.CodigoAsada);
        Assert.Equal(new[] { "Ana Admin", "Beto Operador" }, r.Valor.Usuarios.Select(u => u.Nombre).ToArray());
    }

    [Fact]
    public async Task Listar_AdministradorNoPuedePedirOtraAsada()
    {
        var r = await new ListarUsuariosCasoUso(gestor, organizaciones, Asada(1)).EjecutarAsync(organizacionId: 2);

        Assert.False(r.EsExitoso);
        Assert.Contains("otra ASADA", r.Errores[0]);
    }

    [Fact]
    public async Task Listar_SuperadministradorDebeElegirAsada()
    {
        var sinElegir = await new ListarUsuariosCasoUso(gestor, organizaciones, Superadmin()).EjecutarAsync();
        var elegida = await new ListarUsuariosCasoUso(gestor, organizaciones, Superadmin()).EjecutarAsync(organizacionId: 2);

        Assert.False(sinElegir.EsExitoso);
        Assert.True(elegida.EsExitoso);
        Assert.Equal("Ximena Admin", Assert.Single(elegida.Valor!.Usuarios).Nombre);
    }

    [Fact]
    public async Task Obtener_UsuarioDeOtraAsadaNoExiste()
    {
        var r = await new ObtenerUsuarioCasoUso(gestor, Asada(1), ComoAna()).EjecutarAsync(ximena.Id);

        Assert.False(r.EsExitoso);
        Assert.Equal("El usuario no existe.", r.Errores[0]);
    }

    [Fact]
    public async Task Obtener_IndicaSiEsElMismoUsuario()
    {
        var caso = new ObtenerUsuarioCasoUso(gestor, Asada(1), ComoAna());

        Assert.True((await caso.EjecutarAsync(ana.Id)).Valor!.EsUstedMismo);
        Assert.False((await caso.EjecutarAsync(beto.Id)).Valor!.EsUstedMismo);
    }

    // ---- Crear ----

    private CrearUsuarioCasoUso Crear(ProveedorOrganizacionFalso p) => new(gestor, organizaciones, generador, p);

    [Fact]
    public async Task Crear_GeneraContrasenaTemporalYObligaACambiarla()
    {
        var r = await Crear(Asada(1)).EjecutarAsync(new CrearUsuarioSolicitud(null, "  Carla Consulta ", " Carla@CB.test ", RolesUsuario.Consulta));

        Assert.True(r.EsExitoso);
        Assert.Equal("abcd-2345-efgh", r.Valor!.ContrasenaTemporal);
        var creado = gestor.Usuarios.Single(u => u.Id == r.Valor.UsuarioId);
        Assert.Equal(1, creado.OrganizacionId);
        Assert.Equal("Carla Consulta", creado.Nombre);
        Assert.Equal("carla@cb.test", creado.Correo);
        Assert.Equal(RolesUsuario.Consulta, creado.Rol);
        Assert.True(creado.DebeCambiarContrasena);
    }

    [Fact]
    public async Task Crear_AnotaLaCreacionEnLaBitacoraSinLaContrasena()
    {
        var r = await Crear(Asada(1)).EjecutarAsync(new CrearUsuarioSolicitud(null, "Carla", "carla@cb.test", RolesUsuario.Operador));

        var (id, org, bitacora) = gestor.Bitacora.Last();
        Assert.Equal(r.Valor!.UsuarioId, id);
        Assert.Equal(1, org);
        Assert.Equal(AccionAuditoria.Creacion, bitacora.Accion);
        Assert.Contains(new CambioAuditoria("Rol", null, RolesUsuario.Operador), bitacora.Cambios);
        Assert.DoesNotContain(bitacora.Cambios, c => (c.Despues ?? "").Contains("abcd-2345-efgh"));
    }

    [Theory]
    [InlineData("", "carla@cb.test", "Operador", "El nombre es obligatorio.")]
    [InlineData("Carla", "", "Operador", "El correo es obligatorio.")]
    [InlineData("Carla", "no-es-correo", "Operador", "El correo no es valido.")]
    [InlineData("Carla", "carla@cb.test", "Superadministrador", "Elija el rol del usuario.")]
    [InlineData("Carla", "carla@cb.test", null, "Elija el rol del usuario.")]
    public async Task Crear_ValidaLosDatos(string nombre, string correo, string? rol, string error)
    {
        var r = await Crear(Asada(1)).EjecutarAsync(new CrearUsuarioSolicitud(null, nombre, correo, rol));

        Assert.False(r.EsExitoso);
        Assert.Contains(error, r.Errores);
        Assert.Equal(3, gestor.Usuarios.Count);
    }

    [Fact]
    public async Task Crear_CorreoRepetidoEnCualquierAsadaSeRechaza()
    {
        var r = await Crear(Asada(1)).EjecutarAsync(new CrearUsuarioSolicitud(null, "Otra", "XIMENA@sr.test", RolesUsuario.Operador));

        Assert.False(r.EsExitoso);
        Assert.Equal("Ya existe un usuario con ese correo.", r.Errores[0]);
    }

    [Fact]
    public async Task Crear_SuperadministradorCreaEnLaAsadaElegida()
    {
        var r = await Crear(Superadmin()).EjecutarAsync(new CrearUsuarioSolicitud(2, "Nuevo Admin", "nuevo@sr.test", RolesUsuario.Administrador));

        Assert.True(r.EsExitoso);
        Assert.Equal(2, gestor.Usuarios.Single(u => u.Id == r.Valor!.UsuarioId).OrganizacionId);
    }

    [Fact]
    public async Task Crear_EnAsadaInactivaSeRechaza()
    {
        organizaciones.Organizaciones[1].Estado = EstadoOrganizacion.Inactiva;

        var r = await Crear(Superadmin()).EjecutarAsync(new CrearUsuarioSolicitud(2, "Nuevo", "nuevo@sr.test", RolesUsuario.Operador));

        Assert.False(r.EsExitoso);
        Assert.Equal("La ASADA esta inactiva.", r.Errores[0]);
    }

    // ---- Actualizar ----

    private ActualizarUsuarioCasoUso Actualizar(ProveedorOrganizacionFalso? p = null, ProveedorUsuarioFalso? u = null)
        => new(gestor, p ?? Asada(1), u ?? ComoAna());

    [Fact]
    public async Task Actualizar_CambiaRolYAnotaAntesYDespues()
    {
        var r = await Actualizar().EjecutarAsync(new ActualizarUsuarioSolicitud(null, beto.Id, "Beto Operador", "beto@cb.test", RolesUsuario.Administrador));

        Assert.True(r.EsExitoso);
        Assert.Equal(RolesUsuario.Administrador, beto.Rol);
        Assert.Equal(1, beto.SesionesInvalidadas);
        var cambio = Assert.Single(gestor.Bitacora.Last().Bitacora.Cambios);
        Assert.Equal(new CambioAuditoria("Rol", "Operador", "Administrador"), cambio);
    }

    [Fact]
    public async Task Actualizar_SinCambiosNoGuardaNada()
    {
        var r = await Actualizar().EjecutarAsync(new ActualizarUsuarioSolicitud(null, beto.Id, "Beto Operador", "BETO@cb.test", RolesUsuario.Operador));

        Assert.True(r.EsExitoso);
        Assert.Empty(gestor.Bitacora);
    }

    [Fact]
    public async Task Actualizar_NoPuedeCambiarSuPropioRol()
    {
        gestor.Agregar(1, "Otra Admin", RolesUsuario.Administrador);

        var r = await Actualizar().EjecutarAsync(new ActualizarUsuarioSolicitud(null, ana.Id, "Ana Admin", "ana@cb.test", RolesUsuario.Operador));

        Assert.False(r.EsExitoso);
        Assert.Contains("propio rol", r.Errores[0]);
    }

    [Fact]
    public async Task Actualizar_SiPuedeCambiarSuPropioNombre()
    {
        var r = await Actualizar().EjecutarAsync(new ActualizarUsuarioSolicitud(null, ana.Id, "Ana Maria", "ana@cb.test", RolesUsuario.Administrador));

        Assert.True(r.EsExitoso);
        Assert.Equal("Ana Maria", ana.Nombre);
    }

    [Fact]
    public async Task Actualizar_NoDejaLaAsadaSinAdministrador()
    {
        // El Superadministrador intenta quitarle el rol a la unica Administradora de San Rafael.
        var r = await Actualizar(Superadmin(), ComoSuperadmin())
            .EjecutarAsync(new ActualizarUsuarioSolicitud(2, ximena.Id, "Ximena Admin", "ximena@sr.test", RolesUsuario.Operador));

        Assert.False(r.EsExitoso);
        Assert.Contains("al menos un Administrador activo", r.Errores[0]);
        Assert.Equal(RolesUsuario.Administrador, ximena.Rol);
    }

    [Fact]
    public async Task Actualizar_ConOtroAdministradorActivoSiPermiteQuitarRol()
    {
        var otra = gestor.Agregar(1, "Otra Admin", RolesUsuario.Administrador);

        var r = await Actualizar().EjecutarAsync(new ActualizarUsuarioSolicitud(null, otra.Id, otra.Nombre, otra.Correo, RolesUsuario.Consulta));

        Assert.True(r.EsExitoso);
    }

    [Fact]
    public async Task Actualizar_CorreoDeOtroUsuarioSeRechaza()
    {
        var r = await Actualizar().EjecutarAsync(new ActualizarUsuarioSolicitud(null, beto.Id, "Beto", "ana@cb.test", RolesUsuario.Operador));

        Assert.False(r.EsExitoso);
        Assert.Equal("Ya existe un usuario con ese correo.", r.Errores[0]);
    }

    [Fact]
    public async Task Actualizar_UsuarioDeOtraAsadaNoExiste()
    {
        var r = await Actualizar().EjecutarAsync(new ActualizarUsuarioSolicitud(null, ximena.Id, "X", "x@sr.test", RolesUsuario.Operador));

        Assert.False(r.EsExitoso);
        Assert.Equal("El usuario no existe.", r.Errores[0]);
    }

    // ---- Activar / desactivar ----

    private CambiarEstadoUsuarioCasoUso Estado(ProveedorOrganizacionFalso? p = null, ProveedorUsuarioFalso? u = null)
        => new(gestor, p ?? Asada(1), u ?? ComoAna());

    [Fact]
    public async Task Desactivar_CierraSesionesYAnota()
    {
        var r = await Estado().EjecutarAsync(new CambiarEstadoUsuarioSolicitud(null, beto.Id, Activar: false));

        Assert.True(r.EsExitoso);
        Assert.Equal(EstadoUsuario.Inactivo, beto.Estado);
        Assert.Equal(1, beto.SesionesInvalidadas);
        Assert.Equal(new CambioAuditoria("Estado", "Activo", "Inactivo"), Assert.Single(gestor.Bitacora.Last().Bitacora.Cambios));
    }

    [Fact]
    public async Task Desactivar_NoPuedeDesactivarseASiMismo()
    {
        gestor.Agregar(1, "Otra Admin", RolesUsuario.Administrador);

        var r = await Estado().EjecutarAsync(new CambiarEstadoUsuarioSolicitud(null, ana.Id, Activar: false));

        Assert.False(r.EsExitoso);
        Assert.Equal("No puede desactivar su propio usuario.", r.Errores[0]);
    }

    [Fact]
    public async Task Desactivar_NoDejaLaAsadaSinAdministrador()
    {
        var r = await Estado(Superadmin(), ComoSuperadmin()).EjecutarAsync(new CambiarEstadoUsuarioSolicitud(2, ximena.Id, Activar: false));

        Assert.False(r.EsExitoso);
        Assert.Contains("al menos un Administrador activo", r.Errores[0]);
    }

    [Fact]
    public async Task Reactivar_UsuarioInactivo()
    {
        beto.Estado = EstadoUsuario.Inactivo;

        var r = await Estado().EjecutarAsync(new CambiarEstadoUsuarioSolicitud(null, beto.Id, Activar: true));

        Assert.True(r.EsExitoso);
        Assert.Equal(EstadoUsuario.Activo, beto.Estado);
    }

    [Fact]
    public async Task CambiarEstado_AlMismoEstadoSeRechaza()
    {
        var r = await Estado().EjecutarAsync(new CambiarEstadoUsuarioSolicitud(null, beto.Id, Activar: true));

        Assert.False(r.EsExitoso);
        Assert.Equal("El usuario ya esta activo.", r.Errores[0]);
    }

    // ---- Restablecer contrasena y desbloquear ----

    [Fact]
    public async Task Restablecer_GeneraTemporalDesbloqueaYCierraSesiones()
    {
        beto.BloqueadoHasta = DateTimeOffset.UtcNow.AddMinutes(10);
        beto.IntentosFallidos = 5;

        var r = await new RestablecerContrasenaCasoUso(gestor, generador, Asada(1), ComoAna())
            .EjecutarAsync(new UsuarioObjetivoSolicitud(null, beto.Id));

        Assert.True(r.EsExitoso);
        Assert.Equal("abcd-2345-efgh", r.Valor);
        Assert.Equal("abcd-2345-efgh", beto.Contrasena);
        Assert.True(beto.DebeCambiarContrasena);
        Assert.Null(beto.BloqueadoHasta);
        Assert.Equal(1, beto.SesionesInvalidadas);
        Assert.Equal(new[] { "Contrasena", "Bloqueo" }, gestor.Bitacora.Last().Bitacora.Cambios.Select(c => c.Campo).ToArray());
    }

    [Fact]
    public async Task Restablecer_NoSobreSiMismo()
    {
        var r = await new RestablecerContrasenaCasoUso(gestor, generador, Asada(1), ComoAna())
            .EjecutarAsync(new UsuarioObjetivoSolicitud(null, ana.Id));

        Assert.False(r.EsExitoso);
        Assert.Contains("Cambiar contrasena", r.Errores[0]);
        Assert.Equal(0, generador.Generadas);
    }

    [Fact]
    public async Task Restablecer_UsuarioDesactivadoSeRechaza()
    {
        beto.Estado = EstadoUsuario.Inactivo;

        var r = await new RestablecerContrasenaCasoUso(gestor, generador, Asada(1), ComoAna())
            .EjecutarAsync(new UsuarioObjetivoSolicitud(null, beto.Id));

        Assert.False(r.EsExitoso);
    }

    [Fact]
    public async Task Restablecer_SuperadministradorAyudaAlAdministradorDeUnaAsada()
    {
        var r = await new RestablecerContrasenaCasoUso(gestor, generador, Superadmin(), ComoSuperadmin())
            .EjecutarAsync(new UsuarioObjetivoSolicitud(2, ximena.Id));

        Assert.True(r.EsExitoso);
        Assert.True(ximena.DebeCambiarContrasena);
        Assert.Equal(2, gestor.Bitacora.Last().OrganizacionId);
    }

    [Fact]
    public async Task Desbloquear_QuitaBloqueo()
    {
        beto.BloqueadoHasta = DateTimeOffset.UtcNow.AddMinutes(10);
        beto.IntentosFallidos = 5;

        var r = await new DesbloquearUsuarioCasoUso(gestor, Asada(1)).EjecutarAsync(new UsuarioObjetivoSolicitud(null, beto.Id));

        Assert.True(r.EsExitoso);
        Assert.Null(beto.BloqueadoHasta);
        Assert.Equal(0, beto.IntentosFallidos);
        Assert.Equal("Contrasena123", beto.Contrasena);
    }

    [Fact]
    public async Task Desbloquear_UsuarioNoBloqueadoSeRechaza()
    {
        var r = await new DesbloquearUsuarioCasoUso(gestor, Asada(1)).EjecutarAsync(new UsuarioObjetivoSolicitud(null, beto.Id));

        Assert.False(r.EsExitoso);
        Assert.Equal("El usuario no esta bloqueado.", r.Errores[0]);
    }

    // ---- Cambiar la propia contrasena ----

    private CambiarContrasenaPropiaCasoUso CambiarPropia(int? usuarioId) => new(gestor, new ProveedorUsuarioFalso(usuarioId, "x"));

    [Fact]
    public async Task CambiarPropia_QuitaLaMarcaDeTemporal()
    {
        beto.DebeCambiarContrasena = true;

        var r = await CambiarPropia(beto.Id).EjecutarAsync(new CambiarContrasenaSolicitud("Contrasena123", "rio Celeste 2026", "rio Celeste 2026"));

        Assert.True(r.EsExitoso);
        Assert.False(beto.DebeCambiarContrasena);
        Assert.Equal("rio Celeste 2026", beto.Contrasena);
        Assert.Contains("temporal", gestor.Bitacora.Last().Bitacora.Cambios[0].Despues);
    }

    [Fact]
    public async Task CambiarPropia_ContrasenaActualIncorrecta()
    {
        var r = await CambiarPropia(beto.Id).EjecutarAsync(new CambiarContrasenaSolicitud("otra", "rio Celeste 2026", "rio Celeste 2026"));

        Assert.False(r.EsExitoso);
        Assert.Equal("La contrasena actual no es correcta.", r.Errores[0]);
    }

    [Theory]
    [InlineData("corta1", "corta1", "La contrasena nueva debe tener al menos 10 caracteres.")]
    [InlineData("solamenteletras", "solamenteletras", "La contrasena nueva debe incluir al menos un numero.")]
    [InlineData("1234567890", "1234567890", "La contrasena nueva debe incluir al menos una letra.")]
    [InlineData("rio Celeste 2026", "rio celeste 2026", "La confirmacion no coincide con la contrasena nueva.")]
    [InlineData("Contrasena123", "Contrasena123", "La contrasena nueva debe ser distinta de la actual.")]
    [InlineData("beto@cb.test99", "beto@cb.test99", "La contrasena nueva no puede contener su correo.")]
    public async Task CambiarPropia_ValidaLaPolitica(string nueva, string confirmacion, string error)
    {
        var r = await CambiarPropia(beto.Id).EjecutarAsync(new CambiarContrasenaSolicitud("Contrasena123", nueva, confirmacion));

        Assert.False(r.EsExitoso);
        Assert.Contains(error, r.Errores);
        Assert.Equal("Contrasena123", beto.Contrasena);
    }

    [Fact]
    public async Task CambiarPropia_SinSesionSeRechaza()
    {
        var r = await CambiarPropia(null).EjecutarAsync(new CambiarContrasenaSolicitud("a", "rio Celeste 2026", "rio Celeste 2026"));

        Assert.False(r.EsExitoso);
    }

    // ---- ASADAs de la plataforma ----

    private RegistrarAsadaCasoUso Registrar(ProveedorOrganizacionFalso p) => new(organizaciones, gestor, generador, p);

    [Fact]
    public async Task RegistrarAsada_CreaLaAsadaYSuPrimerAdministrador()
    {
        var r = await Registrar(Superadmin()).EjecutarAsync(new RegistrarAsadaSolicitud(" lp ", " ASADA Las Palmas ", "Laura Admin", "Laura@LP.test"));

        Assert.True(r.EsExitoso);
        var asada = organizaciones.Organizaciones.Single(o => o.Id == r.Valor!.OrganizacionId);
        Assert.Equal("LP", asada.Codigo);
        Assert.Equal("ASADA Las Palmas", asada.Nombre);
        var admin = gestor.Usuarios.Single(u => u.Id == r.Valor!.AdministradorId);
        Assert.Equal(asada.Id, admin.OrganizacionId);
        Assert.Equal(RolesUsuario.Administrador, admin.Rol);
        Assert.Equal("laura@lp.test", admin.Correo);
        Assert.True(admin.DebeCambiarContrasena);
        Assert.Equal("abcd-2345-efgh", r.Valor!.ContrasenaTemporal);
    }

    [Fact]
    public async Task RegistrarAsada_SoloElSuperadministrador()
    {
        var r = await Registrar(Asada(1)).EjecutarAsync(new RegistrarAsadaSolicitud("LP", "Las Palmas", "Laura", "laura@lp.test"));

        Assert.False(r.EsExitoso);
        Assert.Equal(2, organizaciones.Organizaciones.Count);
    }

    [Fact]
    public async Task RegistrarAsada_CodigoRepetidoNoGuardaNada()
    {
        var r = await Registrar(Superadmin()).EjecutarAsync(new RegistrarAsadaSolicitud("cb", "Otra", "Laura", "laura@lp.test"));

        Assert.False(r.EsExitoso);
        Assert.Equal("Ya existe una ASADA con el codigo CB.", r.Errores[0]);
        Assert.Equal(0, organizaciones.Guardados);
        Assert.Equal(3, gestor.Usuarios.Count);
    }

    [Fact]
    public async Task RegistrarAsada_CorreoDelAdministradorRepetidoNoGuardaNada()
    {
        var r = await Registrar(Superadmin()).EjecutarAsync(new RegistrarAsadaSolicitud("LP", "Las Palmas", "Laura", "ana@cb.test"));

        Assert.False(r.EsExitoso);
        Assert.Equal(0, organizaciones.Guardados);
    }

    [Fact]
    public async Task RegistrarAsada_ValidaTodosLosCampos()
    {
        var r = await Registrar(Superadmin()).EjecutarAsync(new RegistrarAsadaSolicitud("C1", "", "", "x"));

        Assert.False(r.EsExitoso);
        Assert.Equal(4, r.Errores.Count);
        Assert.Contains("El nombre del Administrador es obligatorio.", r.Errores);
        Assert.Contains("El correo del Administrador no es valido.", r.Errores);
    }

    [Fact]
    public async Task ListarAsadas_SoloElSuperadministrador()
    {
        Assert.False((await new ListarOrganizacionesCasoUso(organizaciones, Asada(1)).EjecutarAsync()).EsExitoso);
        Assert.Equal(2, (await new ListarOrganizacionesCasoUso(organizaciones, Superadmin()).EjecutarAsync()).Valor!.Count);
    }

    [Fact]
    public void GeneradorContrasena_CumpleLaPoliticaYVaria()
    {
        var g = new GeneradorContrasena();
        var generadas = Enumerable.Range(0, 200).Select(_ => g.GenerarTemporal()).ToList();

        Assert.All(generadas, c => Assert.True(Dominio.Reglas.ReglasContrasena.EsValida(c), c));
        Assert.All(generadas, c => Assert.Equal(14, c.Length));
        Assert.True(generadas.Distinct().Count() > 195);
    }
}
