using Asada.Aplicacion.Abonados.Dtos;
using Asada.Aplicacion.Common;
using Asada.Dominio.Entidades;

namespace Asada.Aplicacion.Abonados;

public class ObtenerExpedienteAbonadoCasoUso(IRepositorioAbonados repositorio)
{
    public async Task<Resultado<ExpedienteAbonadoDto>> EjecutarAsync(int abonadoId, CancellationToken ct = default)
    {
        var abonado = await repositorio.ObtenerConExpedienteAsync(abonadoId, ct);
        return abonado is null
            ? Resultado<ExpedienteAbonadoDto>.Falla("El abonado no existe.")
            : Resultado<ExpedienteAbonadoDto>.Ok(Mapear(abonado));
    }

    private static ExpedienteAbonadoDto Mapear(Abonado a) => new(
        a.Id, a.Codigo, a.TipoIdentificacion, a.Identificacion, a.Nombre,
        a.Telefono, a.Correo, a.Direccion, a.Estado, a.FechaCreacion,
        a.Propiedades
            .OrderBy(p => p.Id)
            .Select(p => new PropiedadDto(
                p.Id, p.Direccion, p.Provincia, p.Canton, p.Distrito, p.Referencia, p.Estado,
                p.Servicios
                    .OrderBy(s => s.Id)
                    .Select(s => new ServicioDto(
                        s.Id, s.FechaInicio, s.Estado, s.Observaciones,
                        s.EsMoroso, s.MensualidadesPendientes, s.MontoPendiente))
                    .ToList()))
            .ToList());
}
