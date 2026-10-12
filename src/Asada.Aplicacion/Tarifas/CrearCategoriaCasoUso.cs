using Asada.Aplicacion.Abonados;
using Asada.Aplicacion.Common;
using Asada.Aplicacion.Tarifas.Dtos;
using Asada.Dominio.Entidades;

namespace Asada.Aplicacion.Tarifas;

public class CrearCategoriaCasoUso(IRepositorioTarifas repositorio, IProveedorOrganizacion proveedorOrganizacion)
{
    public async Task<Resultado<int>> EjecutarAsync(CrearCategoriaSolicitud s, CancellationToken ct = default)
    {
        if (proveedorOrganizacion.OrganizacionId is not int organizacionId)
            return Resultado<int>.Falla("Su usuario no pertenece a ninguna ASADA; no puede crear categorias.");

        var errores = new List<string>();
        Validacion.Requerido(errores, s.Nombre, 60, "El nombre de la categoria");
        Validacion.Largo(errores, Validacion.Limpiar(s.Descripcion), 200, "La descripcion");
        if (errores.Count > 0) return Resultado<int>.Falla(errores);

        var nombre = s.Nombre!.Trim();
        if (await repositorio.ExisteCategoriaConNombreAsync(nombre, ct))
            return Resultado<int>.Falla($"Ya existe una categoria llamada \"{nombre}\".");

        var categoria = new CategoriaServicio
        {
            OrganizacionId = organizacionId,
            Nombre = nombre,
            Descripcion = Validacion.Limpiar(s.Descripcion),
        };
        await repositorio.AgregarCategoriaAsync(categoria, ct);
        return Resultado<int>.Ok(categoria.Id);
    }
}
