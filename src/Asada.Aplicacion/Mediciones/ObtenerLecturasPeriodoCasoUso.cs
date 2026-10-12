using Asada.Aplicacion.Common;
using Asada.Aplicacion.Mediciones.Dtos;
using Asada.Dominio.Reglas;

namespace Asada.Aplicacion.Mediciones;

/// <summary>
/// Estado de la toma de lecturas de un periodo: que servicios ya tienen lectura y cuales no.
/// Cuando la ventana (ultima semana del mes) ya paso, los servicios sin lectura son la alerta.
/// </summary>
public class ObtenerLecturasPeriodoCasoUso(IRepositorioMediciones repositorio, IProveedorFecha fecha)
{
    public async Task<Resultado<LecturasPeriodoDto>> EjecutarAsync(int anio, int mes, CancellationToken ct = default)
    {
        if (!ReglasPeriodo.EsMesValido(anio, mes))
            return Resultado<LecturasPeriodoDto>.Falla("El periodo indicado no es valido.");

        var filas = await repositorio.ListarFilasDelPeriodoAsync(anio, mes, ct);

        return Resultado<LecturasPeriodoDto>.Ok(new LecturasPeriodoDto(
            anio, mes,
            ReglasPeriodo.InicioVentana(anio, mes),
            ReglasPeriodo.FinVentana(anio, mes),
            ReglasPeriodo.EstadoVentana(anio, mes, fecha.Hoy),
            filas));
    }
}
