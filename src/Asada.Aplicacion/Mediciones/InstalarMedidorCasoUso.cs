using Asada.Aplicacion.Abonados;
using Asada.Aplicacion.Common;
using Asada.Aplicacion.Mediciones.Dtos;
using Asada.Dominio.Entidades;
using Asada.Dominio.Enums;

namespace Asada.Aplicacion.Mediciones;

public class InstalarMedidorCasoUso(
    IRepositorioMediciones repositorio, IProveedorOrganizacion proveedorOrganizacion, IProveedorFecha fecha)
{
    public async Task<Resultado<int>> EjecutarAsync(InstalarMedidorSolicitud s, CancellationToken ct = default)
    {
        if (proveedorOrganizacion.OrganizacionId is not int organizacionId)
            return Resultado<int>.Falla("Su usuario no pertenece a ninguna ASADA; no puede registrar medidores.");

        var errores = new List<string>();
        Validacion.Requerido(errores, s.NumeroSerie, 50, "El numero de serie");
        if (s.LecturaInicial < 0) errores.Add("La lectura inicial no puede ser negativa.");
        if (s.FechaInstalacion == default) errores.Add("La fecha de instalacion es obligatoria.");
        else if (s.FechaInstalacion > fecha.Hoy) errores.Add("La fecha de instalacion no puede ser futura.");
        Validacion.Largo(errores, s.Observaciones, 400, "Las observaciones");
        if (errores.Count > 0) return Resultado<int>.Falla(errores);

        var servicio = await repositorio.ObtenerServicioAsync(s.ServicioId, ct);
        if (servicio is null) return Resultado<int>.Falla("El servicio no existe.");
        if (servicio.Estado != EstadoRegistro.Activo)
            return Resultado<int>.Falla("El servicio esta inactivo; no se le puede instalar un medidor.");

        var existentes = await repositorio.ListarMedidoresDeServicioAsync(s.ServicioId, ct);

        if (existentes.Any(m => m.Estado == EstadoMedidor.Activo))
            return Resultado<int>.Falla(
                "El servicio ya tiene un medidor activo. Para cambiarlo, registre primero el retiro del medidor actual.");

        // El medidor nuevo no puede instalarse antes de que se haya retirado el anterior.
        var ultimoRetiro = existentes.Max(m => m.FechaRetiro);
        if (ultimoRetiro is DateOnly retiro && s.FechaInstalacion < retiro)
            return Resultado<int>.Falla(
                $"La fecha de instalacion no puede ser anterior al retiro del medidor anterior ({retiro:dd/MM/yyyy}).");

        var serie = s.NumeroSerie.Trim();
        if (await repositorio.ExisteNumeroSerieAsync(serie, ct))
            return Resultado<int>.Falla("Ya existe un medidor registrado con ese numero de serie.");

        var medidor = new Medidor
        {
            OrganizacionId = organizacionId,
            ServicioId = s.ServicioId,
            NumeroSerie = serie,
            FechaInstalacion = s.FechaInstalacion,
            LecturaInicial = s.LecturaInicial,
            Observaciones = Validacion.Limpiar(s.Observaciones),
        };

        await repositorio.AgregarMedidorAsync(medidor, ct);
        return Resultado<int>.Ok(medidor.Id);
    }
}
