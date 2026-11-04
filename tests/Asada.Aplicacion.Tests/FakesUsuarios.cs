using Asada.Aplicacion.Common;
using Asada.Aplicacion.Organizaciones;
using Asada.Aplicacion.Usuarios;
using Asada.Dominio.Entidades;
using Asada.Dominio.Enums;

namespace Asada.Aplicacion.Tests;

public sealed class GeneradorContrasenaFijo(string temporal = "abcd-2345-efgh") : IGeneradorContrasena
{
    public int Generadas { get; private set; }
    public string GenerarTemporal() { Generadas++; return temporal; }
}

/// <summary>Reemplaza a GestorUsuarios (ASP.NET Core Identity) en las pruebas de casos de uso.</summary>
public sealed class GestorUsuariosEnMemoria : IGestorUsuarios
{
    public sealed class UsuarioFalso
    {
        public int Id;
        public int? OrganizacionId;
        public string Nombre = "";
        public string Correo = "";
        public string Rol = RolesUsuario.Operador;
        public EstadoUsuario Estado = EstadoUsuario.Activo;
        public DateTimeOffset? BloqueadoHasta;
        public int IntentosFallidos;
        public bool DebeCambiarContrasena;
        public string Contrasena = "Contrasena123";
        public int SesionesInvalidadas;
    }

    public List<UsuarioFalso> Usuarios { get; } = [];
    public List<(int UsuarioId, int? OrganizacionId, RegistroBitacora Bitacora)> Bitacora { get; } = [];

    public UsuarioFalso Agregar(int? organizacionId, string nombre, string rol, EstadoUsuario estado = EstadoUsuario.Activo, string? correo = null)
    {
        var u = new UsuarioFalso
        {
            Id = Usuarios.Count + 1, OrganizacionId = organizacionId, Nombre = nombre, Rol = rol, Estado = estado,
            Correo = correo ?? $"{nombre.Split(' ')[0].ToLowerInvariant()}{Usuarios.Count + 1}@asada.test",
        };
        Usuarios.Add(u);
        return u;
    }

    private static UsuarioDto Dto(UsuarioFalso u) => new(u.Id, u.OrganizacionId, u.Nombre, u.Correo, u.Rol, u.Estado,
        u.BloqueadoHasta, u.IntentosFallidos, u.DebeCambiarContrasena, null, new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc));

    private UsuarioFalso? De(int org, int id) => Usuarios.FirstOrDefault(u => u.Id == id && u.OrganizacionId == org);

    public Task<IReadOnlyList<UsuarioDto>> ListarAsync(int organizacionId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<UsuarioDto>>(Usuarios.Where(u => u.OrganizacionId == organizacionId).Select(Dto).ToList());

    public Task<UsuarioDto?> ObtenerAsync(int organizacionId, int usuarioId, CancellationToken ct = default)
        => Task.FromResult(De(organizacionId, usuarioId) is { } u ? Dto(u) : null);

    public Task<UsuarioDto?> ObtenerPropioAsync(int usuarioId, CancellationToken ct = default)
        => Task.FromResult(Usuarios.FirstOrDefault(u => u.Id == usuarioId) is { } u ? Dto(u) : null);

    public Task<bool> ExisteCorreoAsync(string correo, int? excluirUsuarioId = null, CancellationToken ct = default)
        => Task.FromResult(Usuarios.Any(u => u.Id != excluirUsuarioId && string.Equals(u.Correo, correo, StringComparison.OrdinalIgnoreCase)));

    public Task<int> ContarAdministradoresActivosAsync(int organizacionId, CancellationToken ct = default)
        => Task.FromResult(Usuarios.Count(u => u.OrganizacionId == organizacionId && u.Rol == RolesUsuario.Administrador && u.Estado == EstadoUsuario.Activo));

    public Task<Resultado<int>> CrearAsync(DatosNuevoUsuario d, RegistroBitacora bitacora, CancellationToken ct = default)
    {
        var u = Agregar(d.OrganizacionId, d.Nombre, d.Rol, correo: d.Correo);
        u.Contrasena = d.ContrasenaTemporal;
        u.DebeCambiarContrasena = true;
        Bitacora.Add((u.Id, d.OrganizacionId, bitacora));
        return Task.FromResult(Resultado<int>.Ok(u.Id));
    }

    public Task<Resultado> ActualizarAsync(int org, int id, string nombre, string correo, string rol, RegistroBitacora bitacora, CancellationToken ct = default)
    {
        var u = De(org, id)!;
        if (u.Rol != rol) u.SesionesInvalidadas++;
        u.Nombre = nombre; u.Correo = correo; u.Rol = rol;
        Bitacora.Add((id, org, bitacora));
        return Task.FromResult(Resultado.Ok());
    }

    public Task<Resultado> CambiarEstadoAsync(int org, int id, EstadoUsuario estado, RegistroBitacora bitacora, CancellationToken ct = default)
    {
        var u = De(org, id)!;
        u.Estado = estado; u.SesionesInvalidadas++;
        Bitacora.Add((id, org, bitacora));
        return Task.FromResult(Resultado.Ok());
    }

    public Task<Resultado> RestablecerContrasenaAsync(int org, int id, string temporal, RegistroBitacora bitacora, CancellationToken ct = default)
    {
        var u = De(org, id)!;
        u.Contrasena = temporal; u.DebeCambiarContrasena = true; u.BloqueadoHasta = null; u.IntentosFallidos = 0; u.SesionesInvalidadas++;
        Bitacora.Add((id, org, bitacora));
        return Task.FromResult(Resultado.Ok());
    }

    public Task<Resultado> DesbloquearAsync(int org, int id, RegistroBitacora bitacora, CancellationToken ct = default)
    {
        var u = De(org, id)!;
        u.BloqueadoHasta = null; u.IntentosFallidos = 0;
        Bitacora.Add((id, org, bitacora));
        return Task.FromResult(Resultado.Ok());
    }

    public Task<Resultado> CambiarContrasenaPropiaAsync(int id, string actual, string nueva, RegistroBitacora bitacora, CancellationToken ct = default)
    {
        var u = Usuarios.First(x => x.Id == id);
        if (u.Contrasena != actual) return Task.FromResult(Resultado.Falla("La contrasena actual no es correcta."));
        u.Contrasena = nueva; u.DebeCambiarContrasena = false;
        Bitacora.Add((id, u.OrganizacionId, bitacora));
        return Task.FromResult(Resultado.Ok());
    }
}

public sealed class RepositorioOrganizacionesEnMemoria : IRepositorioOrganizaciones
{
    public List<Organizacion> Organizaciones { get; } =
    [
        new Organizacion { Id = 1, Codigo = "CB", Nombre = "ASADA Cuatro Bocas" },
        new Organizacion { Id = 2, Codigo = "SR", Nombre = "ASADA San Rafael" },
    ];
    public int Guardados { get; private set; }

    public Task<IReadOnlyList<OrganizacionResumenDto>> ListarAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<OrganizacionResumenDto>>(Organizaciones
            .Select(o => new OrganizacionResumenDto(o.Id, o.Codigo, o.Nombre, o.Estado, 0, o.FechaCreacion)).ToList());

    public Task<Organizacion?> ObtenerAsync(int organizacionId, CancellationToken ct = default)
        => Task.FromResult(Organizaciones.FirstOrDefault(o => o.Id == organizacionId));

    public Task<bool> ExisteCodigoAsync(string codigo, CancellationToken ct = default)
        => Task.FromResult(Organizaciones.Any(o => o.Codigo == codigo));

    public Task AgregarAsync(Organizacion organizacion, CancellationToken ct = default)
    {
        organizacion.Id = Organizaciones.Max(o => o.Id) + 1;
        Organizaciones.Add(organizacion);
        return Task.CompletedTask;
    }

    public Task GuardarCambiosAsync(CancellationToken ct = default) { Guardados++; return Task.CompletedTask; }
}
