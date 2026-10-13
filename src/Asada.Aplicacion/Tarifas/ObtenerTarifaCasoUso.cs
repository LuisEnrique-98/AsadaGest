using Asada.Aplicacion.Common;
using Asada.Aplicacion.Tarifas.Dtos;
using Asada.Dominio.Reglas;

namespace Asada.Aplicacion.Tarifas;

public class ObtenerTarifaCasoUso(IRepositorioTarifas repositorio, IProveedorFecha fecha)
{
    public async Task<Resultado<TarifaDetalleDto>> EjecutarAsync(int tarifaId, CancellationToken ct = default)
    {
        var t = await repositorio.ObtenerTarifaAsync(tarifaId, ct);
        if (t is null) return Resultado<TarifaDetalleDto>.Falla("La tarifa no existe.");

        var vigentes = MapeoTarifas.IdsVigentes(await repositorio.ListarTarifasAsync(ct), fecha.Hoy);

        return Resultado<TarifaDetalleDto>.Ok(new TarifaDetalleDto(
            t.Id, t.CategoriaId, t.Categoria?.Nombre ?? "", t.VigenteDesde, t.CargoFijo,
            t.ModoHidrante, t.ValorHidrante, t.Observaciones, MapeoTarifas.Tramos(t),
            vigentes.Contains(t.Id), t.VigenteDesde > fecha.Hoy));
    }
}

/// <summary>Calcula, sin guardar nada, cuanto cobraria una tarifa por un consumo dado.
/// Sirve para comprobar una tarifa recien cargada contra el pliego tarifario.</summary>
public class SimularReciboCasoUso(IRepositorioTarifas repositorio)
{
    public async Task<Resultado<DesgloseRecibo>> EjecutarAsync(int tarifaId, decimal consumo, CancellationToken ct = default)
    {
        var t = await repositorio.ObtenerTarifaAsync(tarifaId, ct);
        if (t is null) return Resultado<DesgloseRecibo>.Falla("La tarifa no existe.");

        var r = CalculadoraRecibo.Calcular(t, consumo);
        return r.EsValido
            ? Resultado<DesgloseRecibo>.Ok(r.Desglose!)
            : Resultado<DesgloseRecibo>.Falla(r.Error!);
    }
}
