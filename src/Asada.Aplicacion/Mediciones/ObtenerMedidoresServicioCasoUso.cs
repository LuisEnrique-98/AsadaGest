using Asada.Aplicacion.Common;
using Asada.Aplicacion.Mediciones.Dtos;

namespace Asada.Aplicacion.Mediciones;

public class ObtenerMedidoresServicioCasoUso(IRepositorioMediciones repositorio)
{
    public async Task<Resultado<ServicioMedidoresDto>> EjecutarAsync(int servicioId, CancellationToken ct = default)
    {
        var servicio = await repositorio.ObtenerServicioAsync(servicioId, ct);
        if (servicio?.Propiedad?.Abonado is null)
            return Resultado<ServicioMedidoresDto>.Falla("El servicio no existe.");

        var medidores = await repositorio.ListarMedidoresDeServicioAsync(servicioId, ct);

        return Resultado<ServicioMedidoresDto>.Ok(new ServicioMedidoresDto(
            servicio.Id,
            servicio.Propiedad.Abonado.Id,
            servicio.Propiedad.Abonado.Codigo,
            servicio.Propiedad.Abonado.Nombre,
            servicio.Propiedad.Direccion,
            servicio.Estado,
            medidores
                .Select(m => new MedidorDto(
                    m.Id, m.NumeroSerie, m.FechaInstalacion, m.LecturaInicial,
                    m.Estado, m.FechaRetiro, m.LecturaFinal, m.Observaciones))
                .ToList()));
    }
}
