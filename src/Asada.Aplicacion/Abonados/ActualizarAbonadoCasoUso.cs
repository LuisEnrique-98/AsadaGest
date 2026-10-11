using Asada.Aplicacion.Abonados.Dtos;
using Asada.Aplicacion.Common;
using Asada.Dominio.Reglas;

namespace Asada.Aplicacion.Abonados;

public class ActualizarAbonadoCasoUso(IRepositorioAbonados repositorio)
{
    public async Task<Resultado> EjecutarAsync(ActualizarAbonadoSolicitud s, CancellationToken ct = default)
    {
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

        if (errores.Count > 0) return Resultado.Falla(errores);

        var abonado = await repositorio.ObtenerConExpedienteAsync(s.AbonadoId, ct);
        if (abonado is null) return Resultado.Falla("El abonado no existe.");

        if (await repositorio.ExisteIdentificacionAsync(s.TipoIdentificacion, identificacion, s.AbonadoId, ct))
            return Resultado.Falla("Ya existe otro abonado registrado con esa identificacion.");

        abonado.ActualizarDatos(
            s.TipoIdentificacion, identificacion, s.Nombre.Trim(),
            Validacion.Limpiar(s.Telefono), correo, Validacion.Limpiar(s.Direccion));

        await repositorio.GuardarCambiosAsync(ct);
        return Resultado.Ok();
    }
}
