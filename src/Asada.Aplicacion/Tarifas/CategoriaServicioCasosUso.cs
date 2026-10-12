using Asada.Aplicacion.Common;
using Asada.Aplicacion.Tarifas.Dtos;
using Asada.Dominio.Enums;

namespace Asada.Aplicacion.Tarifas;

/// <summary>Datos para la pantalla de asignar categoria a un servicio.</summary>
public class ObtenerCategoriaServicioCasoUso(IRepositorioTarifas repositorio)
{
    public async Task<Resultado<CategoriaServicioDto>> EjecutarAsync(int servicioId, CancellationToken ct = default)
    {
        var servicio = await repositorio.ObtenerServicioAsync(servicioId, ct);
        if (servicio?.Propiedad?.Abonado is not { } abonado)
            return Resultado<CategoriaServicioDto>.Falla("El servicio no existe.");

        var categorias = (await repositorio.ListarCategoriasAsync(ct))
            .Where(c => c.Estado == EstadoRegistro.Activo || c.Id == servicio.CategoriaId)
            .Select(c => new CategoriaDto(c.Id, c.Nombre, c.Descripcion, c.Estado, 0))
            .ToList();

        return Resultado<CategoriaServicioDto>.Ok(new CategoriaServicioDto(
            servicio.Id, abonado.Id, abonado.Codigo, abonado.Nombre, servicio.Propiedad.Direccion,
            servicio.CategoriaId, categorias));
    }
}

/// <summary>
/// Asigna (o cambia) la categoria tarifaria de un servicio. Solo afecta los recibos que se
/// generen despues: los ya emitidos conservan la categoria y la tarifa con que se calcularon.
/// </summary>
public class AsignarCategoriaServicioCasoUso(IRepositorioTarifas repositorio)
{
    public async Task<Resultado> EjecutarAsync(AsignarCategoriaSolicitud s, CancellationToken ct = default)
    {
        var servicio = await repositorio.ObtenerServicioAsync(s.ServicioId, ct);
        if (servicio is null) return Resultado.Falla("El servicio no existe.");

        var categoria = await repositorio.ObtenerCategoriaAsync(s.CategoriaId, ct);
        if (categoria is null) return Resultado.Falla("Seleccione una categoria valida.");
        if (categoria.Estado != EstadoRegistro.Activo && categoria.Id != servicio.CategoriaId)
            return Resultado.Falla("La categoria esta inactiva.");

        servicio.CategoriaId = categoria.Id;
        await repositorio.GuardarCambiosAsync(ct);
        return Resultado.Ok();
    }
}
