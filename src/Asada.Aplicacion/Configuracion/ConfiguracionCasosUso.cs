using Asada.Aplicacion.Abonados;
using Asada.Aplicacion.Common;
using Asada.Dominio.Entidades;
using Asada.Dominio.Enums;
using Asada.Dominio.Reglas;

namespace Asada.Aplicacion.Configuracion;

public interface IRepositorioConfiguracion
{
    /// <summary>La organizacion del usuario actual (con seguimiento de cambios), o null.</summary>
    Task<Organizacion?> ObtenerOrganizacionAsync(int organizacionId, CancellationToken ct = default);
    Task GuardarCambiosAsync(CancellationToken ct = default);
}

/// <summary>Datos de la ASADA que edita su Administrador: contacto (se imprimen en el recibo) y umbral de consumo anormal.</summary>
public sealed record ConfiguracionDto(
    string Codigo,
    string Nombre,
    string? CedulaJuridica,
    string? Telefono,
    string? Correo,
    string? Direccion,
    decimal PorcentajeConsumoAnormal);

public sealed record ActualizarConfiguracionSolicitud(
    string? CedulaJuridica,
    string? Telefono,
    string? Correo,
    string? Direccion,
    decimal PorcentajeConsumoAnormal);

public class ObtenerConfiguracionCasoUso(IRepositorioConfiguracion repositorio, IProveedorOrganizacion proveedorOrganizacion)
{
    public async Task<Resultado<ConfiguracionDto>> EjecutarAsync(CancellationToken ct = default)
    {
        if (proveedorOrganizacion.OrganizacionId is not int organizacionId)
            return Resultado<ConfiguracionDto>.Falla("Su usuario no pertenece a ninguna ASADA.");

        var o = await repositorio.ObtenerOrganizacionAsync(organizacionId, ct);
        if (o is null) return Resultado<ConfiguracionDto>.Falla("La ASADA no existe.");

        return Resultado<ConfiguracionDto>.Ok(new ConfiguracionDto(
            o.Codigo, o.Nombre, o.CedulaJuridica, o.Telefono, o.Correo, o.Direccion, o.PorcentajeConsumoAnormal));
    }
}

/// <summary>
/// El Administrador actualiza los datos de contacto de su ASADA y el umbral de consumo anormal
/// (correccion C-05: parametro por ASADA). El nombre y el codigo no se cambian aqui: el codigo
/// forma parte de los numeros de abonado y de recibo ya emitidos.
/// </summary>
public class ActualizarConfiguracionCasoUso(IRepositorioConfiguracion repositorio, IProveedorOrganizacion proveedorOrganizacion)
{
    public async Task<Resultado> EjecutarAsync(ActualizarConfiguracionSolicitud s, CancellationToken ct = default)
    {
        if (proveedorOrganizacion.OrganizacionId is not int organizacionId)
            return Resultado.Falla("Su usuario no pertenece a ninguna ASADA.");

        var errores = new List<string>();
        // La cedula juridica se guarda con el formato con que se imprime: 3-101-123456.
        string? cedula = null;
        if (Validacion.Limpiar(s.CedulaJuridica) is string cedulaEscrita)
        {
            if (ReglasIdentificacion.EsValida(TipoIdentificacion.CedulaJuridica, cedulaEscrita))
            {
                var n = ReglasIdentificacion.Normalizar(TipoIdentificacion.CedulaJuridica, cedulaEscrita);
                cedula = $"{n[0]}-{n[1..4]}-{n[4..]}";
            }
            else errores.Add(ReglasIdentificacion.MensajeFormato(TipoIdentificacion.CedulaJuridica));
        }
        Validacion.Largo(errores, Validacion.Limpiar(s.Telefono), 30, "El telefono");
        var correo = Validacion.Limpiar(s.Correo);
        if (correo is not null && (!Validacion.CorreoValido(correo) || correo.Length > 150))
            errores.Add("El correo no es valido.");
        Validacion.Largo(errores, Validacion.Limpiar(s.Direccion), 400, "La direccion");
        if (!ReglasConsumoAnormal.EsUmbralValido(s.PorcentajeConsumoAnormal))
            errores.Add($"El umbral de consumo anormal debe estar entre {ReglasConsumoAnormal.PorcentajeUmbralMinimo:0} % y {ReglasConsumoAnormal.PorcentajeUmbralMaximo:0} %.");
        if (errores.Count > 0) return Resultado.Falla(errores);

        var o = await repositorio.ObtenerOrganizacionAsync(organizacionId, ct);
        if (o is null) return Resultado.Falla("La ASADA no existe.");

        o.CedulaJuridica = cedula;
        o.Telefono = Validacion.Limpiar(s.Telefono);
        o.Correo = correo;
        o.Direccion = Validacion.Limpiar(s.Direccion);
        o.PorcentajeConsumoAnormal = decimal.Round(s.PorcentajeConsumoAnormal, 2);
        await repositorio.GuardarCambiosAsync(ct);
        return Resultado.Ok();
    }
}
