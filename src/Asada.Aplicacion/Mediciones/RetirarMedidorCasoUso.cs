using Asada.Aplicacion.Common;
using Asada.Aplicacion.Mediciones.Dtos;
using Asada.Dominio.Enums;

namespace Asada.Aplicacion.Mediciones;

public class RetirarMedidorCasoUso(IRepositorioMediciones repositorio, IProveedorFecha fecha)
{
    public async Task<Resultado> EjecutarAsync(RetirarMedidorSolicitud s, CancellationToken ct = default)
    {
        var errores = new List<string>();
        if (s.LecturaFinal < 0) errores.Add("La lectura final no puede ser negativa.");
        if (s.FechaRetiro == default) errores.Add("La fecha de retiro es obligatoria.");
        else if (s.FechaRetiro > fecha.Hoy) errores.Add("La fecha de retiro no puede ser futura.");
        if (errores.Count > 0) return Resultado.Falla(errores);

        var medidor = await repositorio.ObtenerMedidorAsync(s.MedidorId, ct);
        if (medidor is null) return Resultado.Falla("El medidor no existe.");
        if (medidor.Estado != EstadoMedidor.Activo) return Resultado.Falla("El medidor ya fue retirado.");

        if (s.FechaRetiro < medidor.FechaInstalacion)
            errores.Add("La fecha de retiro no puede ser anterior a la de instalacion.");
        if (s.LecturaFinal < medidor.LecturaInicial)
            errores.Add("La lectura final no puede ser menor que la lectura inicial del medidor.");

        // Si ya se tomaron lecturas con este medidor, la lectura final no puede quedar por debajo
        // de la ultima ni la fecha de retiro antes de ella: se perderia consumo.
        var ultima = await repositorio.ObtenerUltimaLecturaAsync(medidor.ServicioId, ct);
        if (ultima is not null && ultima.MedidorId == medidor.Id)
        {
            if (s.LecturaFinal < ultima.Lectura)
                errores.Add($"La lectura final no puede ser menor que la ultima lectura registrada ({ultima.Lectura:0.###}).");
            if (s.FechaRetiro < ultima.FechaLectura)
                errores.Add($"La fecha de retiro no puede ser anterior a la ultima lectura ({ultima.FechaLectura:dd/MM/yyyy}).");
        }

        if (errores.Count > 0) return Resultado.Falla(errores);

        medidor.Retirar(s.FechaRetiro, s.LecturaFinal);
        await repositorio.GuardarCambiosAsync(ct);
        return Resultado.Ok();
    }
}
