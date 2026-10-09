using Asada.Aplicacion.Abonados;
using Asada.Aplicacion.Common;
using Asada.Aplicacion.Mediciones.Dtos;
using Asada.Dominio.Entidades;
using Asada.Dominio.Enums;
using Asada.Dominio.Reglas;

namespace Asada.Aplicacion.Mediciones;

/// <summary>
/// El operador anota que hara con un servicio sin lectura en el periodo: leerlo lo antes
/// posible o esperar al mes siguiente. Si ya existe una anotacion para ese servicio y periodo,
/// se actualiza (hay una sola por servicio y periodo).
/// </summary>
public class RegistrarSeguimientoLecturaCasoUso(
    IRepositorioMediciones repositorio, IProveedorOrganizacion proveedorOrganizacion)
{
    public async Task<Resultado> EjecutarAsync(RegistrarSeguimientoLecturaSolicitud s, CancellationToken ct = default)
    {
        if (proveedorOrganizacion.OrganizacionId is not int organizacionId)
            return Resultado.Falla("Su usuario no pertenece a ninguna ASADA; no puede registrar anotaciones.");

        var errores = new List<string>();
        if (!ReglasPeriodo.EsMesValido(s.Anio, s.Mes)) errores.Add("El periodo indicado no es valido.");
        if (!Enum.IsDefined(s.Decision)) errores.Add("Seleccione que se hara con la lectura pendiente.");
        Validacion.Largo(errores, s.Nota, 400, "La nota");
        if (errores.Count > 0) return Resultado.Falla(errores);

        if (await repositorio.ObtenerServicioAsync(s.ServicioId, ct) is null)
            return Resultado.Falla("El servicio no existe.");

        if (await repositorio.ExisteLecturaAsync(s.ServicioId, s.Anio, s.Mes, ct))
            return Resultado.Falla("El servicio ya tiene lectura en ese periodo; no necesita anotacion.");

        var periodo = await repositorio.ObtenerOCrearPeriodoAsync(s.Anio, s.Mes, ct);
        var existente = await repositorio.ObtenerSeguimientoAsync(s.ServicioId, periodo.Id, ct);

        if (existente is null)
        {
            await repositorio.AgregarSeguimientoAsync(new SeguimientoLectura
            {
                OrganizacionId = organizacionId,
                ServicioId = s.ServicioId,
                PeriodoId = periodo.Id,
                Decision = s.Decision,
                Nota = Validacion.Limpiar(s.Nota),
            }, ct);
        }
        else
        {
            existente.Decision = s.Decision;
            existente.Nota = Validacion.Limpiar(s.Nota);
            existente.FechaRegistro = DateTime.UtcNow;
            await repositorio.GuardarCambiosAsync(ct);
        }

        return Resultado.Ok();
    }
}
