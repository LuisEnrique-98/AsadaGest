using Asada.Aplicacion.Abonados.Dtos;
using Asada.Aplicacion.Common;
using Asada.Dominio.Entidades;
using Asada.Dominio.Reglas;

namespace Asada.Aplicacion.Abonados;

public class CrearAbonadoCasoUso(IRepositorioAbonados repositorio, IProveedorOrganizacion proveedorOrganizacion)
{
    public async Task<Resultado<int>> EjecutarAsync(CrearAbonadoSolicitud s, CancellationToken ct = default)
    {
        // Solo un usuario que pertenece a una ASADA puede crear abonados; el Superadministrador
        // gestiona la plataforma, no la operacion de una organizacion.
        if (proveedorOrganizacion.OrganizacionId is not int organizacionId)
            return Resultado<int>.Falla("Su usuario no pertenece a ninguna ASADA; no puede registrar abonados.");

        var errores = new List<string>();
        var identificacion = ReglasIdentificacion.Normalizar(s.TipoIdentificacion, s.Identificacion);

        if (!ReglasIdentificacion.EsValida(s.TipoIdentificacion, s.Identificacion))
            errores.Add(ReglasIdentificacion.MensajeFormato(s.TipoIdentificacion));

        Validacion.Requerido(errores, s.Nombre, 150, "El nombre");
        Validacion.Largo(errores, s.Telefono, 30, "El telefono");
        Validacion.Largo(errores, s.Correo, 150, "El correo");
        Validacion.Largo(errores, s.Direccion, 400, "La direccion");

        var correo = Validacion.Limpiar(s.Correo);
        if (correo is not null && !Validacion.CorreoValido(correo))
            errores.Add("El correo no tiene un formato valido.");

        if (errores.Count > 0) return Resultado<int>.Falla(errores);

        if (await repositorio.ExisteIdentificacionAsync(s.TipoIdentificacion, identificacion, ct: ct))
            return Resultado<int>.Falla("Ya existe un abonado registrado con esa identificacion.");

        var abonado = new Abonado
        {
            OrganizacionId = organizacionId,
            TipoIdentificacion = s.TipoIdentificacion,
            Identificacion = identificacion,
            Nombre = s.Nombre.Trim(),
            Telefono = Validacion.Limpiar(s.Telefono),
            Correo = correo,
            Direccion = Validacion.Limpiar(s.Direccion),
        };

        await repositorio.AgregarAsync(abonado, ct);
        return Resultado<int>.Ok(abonado.Id);
    }
}
