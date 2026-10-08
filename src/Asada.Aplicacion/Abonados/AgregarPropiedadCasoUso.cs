using Asada.Aplicacion.Abonados.Dtos;
using Asada.Aplicacion.Common;

namespace Asada.Aplicacion.Abonados;

public class AgregarPropiedadCasoUso(IRepositorioAbonados repositorio)
{
    public async Task<Resultado<int>> EjecutarAsync(AgregarPropiedadSolicitud s, CancellationToken ct = default)
    {
        var errores = new List<string>();
        Validacion.Requerido(errores, s.Direccion, 400, "La direccion");
        Validacion.Largo(errores, s.Provincia, 80, "La provincia");
        Validacion.Largo(errores, s.Canton, 80, "El canton");
        Validacion.Largo(errores, s.Distrito, 80, "El distrito");
        Validacion.Largo(errores, s.Referencia, 400, "Las senas");
        if (errores.Count > 0) return Resultado<int>.Falla(errores);

        var abonado = await repositorio.ObtenerConExpedienteAsync(s.AbonadoId, ct);
        if (abonado is null) return Resultado<int>.Falla("El abonado no existe.");

        var propiedad = abonado.AgregarPropiedad(
            s.Direccion.Trim(),
            Validacion.Limpiar(s.Provincia), Validacion.Limpiar(s.Canton),
            Validacion.Limpiar(s.Distrito), Validacion.Limpiar(s.Referencia));

        await repositorio.GuardarCambiosAsync(ct);
        return Resultado<int>.Ok(propiedad.Id);
    }
}
