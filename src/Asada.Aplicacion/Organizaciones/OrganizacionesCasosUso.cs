using Asada.Aplicacion.Abonados;
using Asada.Aplicacion.Auditoria;
using Asada.Aplicacion.Common;
using Asada.Aplicacion.Usuarios;
using Asada.Dominio.Entidades;
using Asada.Dominio.Enums;
using Asada.Dominio.Reglas;

namespace Asada.Aplicacion.Organizaciones;

/// <summary>Una ASADA en la lista de la plataforma (solo la ve el Superadministrador).</summary>
public sealed record OrganizacionResumenDto(
    int Id, string Codigo, string Nombre, EstadoOrganizacion Estado, int UsuariosActivos, DateTime FechaCreacion);

public interface IRepositorioOrganizaciones
{
    /// <summary>Todas las ASADAs (el filtro de organizacion deja ver todas solo al Superadministrador).</summary>
    Task<IReadOnlyList<OrganizacionResumenDto>> ListarAsync(CancellationToken ct = default);

    /// <summary>La ASADA, si existe y el usuario actual puede verla (filtro de organizacion).</summary>
    Task<Organizacion?> ObtenerAsync(int organizacionId, CancellationToken ct = default);

    /// <summary>True si el codigo ya lo usa alguna ASADA (sin importar el filtro).</summary>
    Task<bool> ExisteCodigoAsync(string codigo, CancellationToken ct = default);

    Task AgregarAsync(Organizacion organizacion, CancellationToken ct = default);
    Task GuardarCambiosAsync(CancellationToken ct = default);
}

public class ListarOrganizacionesCasoUso(IRepositorioOrganizaciones repositorio, IProveedorOrganizacion proveedorOrganizacion)
{
    public async Task<Resultado<IReadOnlyList<OrganizacionResumenDto>>> EjecutarAsync(CancellationToken ct = default)
    {
        if (!proveedorOrganizacion.EsSuperadministrador)
            return Resultado<IReadOnlyList<OrganizacionResumenDto>>.Falla("Solo el Superadministrador administra las ASADAs de la plataforma.");
        return Resultado<IReadOnlyList<OrganizacionResumenDto>>.Ok(await repositorio.ListarAsync(ct));
    }
}

public sealed record RegistrarAsadaSolicitud(string? Codigo, string? Nombre, string? NombreAdministrador, string? CorreoAdministrador);

public sealed record AsadaRegistradaDto(int OrganizacionId, string Codigo, string Nombre, int AdministradorId, string CorreoAdministrador, string ContrasenaTemporal);

/// <summary>
/// El Superadministrador registra una ASADA nueva junto con su primer Administrador (decision del
/// 4 de noviembre de 2026). Todo se valida antes de guardar nada. Los datos de contacto, el umbral
/// de consumo anormal y el resto de usuarios los completa despues el Administrador de esa ASADA.
/// </summary>
public class RegistrarAsadaCasoUso(
    IRepositorioOrganizaciones repositorio,
    IGestorUsuarios gestor,
    IGeneradorContrasena generador,
    IProveedorOrganizacion proveedorOrganizacion)
{
    public async Task<Resultado<AsadaRegistradaDto>> EjecutarAsync(RegistrarAsadaSolicitud s, CancellationToken ct = default)
    {
        if (!proveedorOrganizacion.EsSuperadministrador)
            return Resultado<AsadaRegistradaDto>.Falla("Solo el Superadministrador registra ASADAs nuevas.");

        var errores = new List<string>();
        var codigo = ReglasOrganizacion.NormalizarCodigo(s.Codigo);
        if (ReglasOrganizacion.ValidarCodigo(codigo) is string errorCodigo) errores.Add(errorCodigo);
        Validacion.Requerido(errores, s.Nombre, ReglasOrganizacion.LargoMaximoNombre, "El nombre de la ASADA");
        var datosAdmin = ValidacionUsuarios.Validar(errores, s.NombreAdministrador, s.CorreoAdministrador, "del Administrador");
        if (errores.Count > 0) return Resultado<AsadaRegistradaDto>.Falla(errores);

        if (await repositorio.ExisteCodigoAsync(codigo, ct))
            errores.Add($"Ya existe una ASADA con el codigo {codigo}.");
        if (await gestor.ExisteCorreoAsync(datosAdmin.Correo, null, ct))
            errores.Add("Ya existe un usuario con ese correo.");
        if (errores.Count > 0) return Resultado<AsadaRegistradaDto>.Falla(errores);

        var organizacion = new Organizacion
        {
            Codigo = codigo,
            Nombre = s.Nombre!.Trim(),
            Estado = EstadoOrganizacion.Activa,
        };
        await repositorio.AgregarAsync(organizacion, ct);
        await repositorio.GuardarCambiosAsync(ct);

        var temporal = generador.GenerarTemporal();
        var creado = await gestor.CrearAsync(
            new DatosNuevoUsuario(organizacion.Id, datosAdmin.Nombre, datosAdmin.Correo, RolesUsuario.Administrador, temporal),
            ValidacionUsuarios.BitacoraCreacion(datosAdmin.Nombre, datosAdmin.Correo, RolesUsuario.Administrador),
            ct);
        if (!creado.EsExitoso)
            return Resultado<AsadaRegistradaDto>.Falla(
                new[] { $"La ASADA {codigo} quedo registrada, pero no se pudo crear su Administrador. Creelo desde Usuarios de esa ASADA." }
                    .Concat(creado.Errores));

        return Resultado<AsadaRegistradaDto>.Ok(new AsadaRegistradaDto(
            organizacion.Id, organizacion.Codigo, organizacion.Nombre, creado.Valor, datosAdmin.Correo, temporal));
    }
}
