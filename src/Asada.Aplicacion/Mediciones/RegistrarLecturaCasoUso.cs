using Asada.Aplicacion.Abonados;
using Asada.Aplicacion.Common;
using Asada.Aplicacion.Mediciones.Dtos;
using Asada.Dominio.Entidades;
using Asada.Dominio.Enums;
using Asada.Dominio.Reglas;

namespace Asada.Aplicacion.Mediciones;

/// <summary>
/// Registra la lectura de un servicio en un periodo y calcula su consumo.
///
/// Una lectura nunca se rechaza por ser "rara": se guarda y se marca para revision manual
/// (RequiereRevision) cuando algo no es normal -- decision de negocio del 9 de octubre de 2026:
/// una omision de lectura es un caso aislado y anormal, y el operador es quien decide.
/// Motivos de revision:
///   - Fuera de ventana: la fecha de lectura no cae en la ultima semana del mes del periodo.
///   - Periodo anterior sin lectura: el servicio se salto al menos un periodo (RB-014); el
///     consumo NO se reparte entre meses, se factura completo y queda marcado.
///   - Consumo anormal: supera el promedio de los ultimos 3 consumos en mas del umbral.
/// </summary>
public class RegistrarLecturaCasoUso(
    IRepositorioMediciones repositorio, IProveedorOrganizacion proveedorOrganizacion, IProveedorFecha fecha)
{
    public async Task<Resultado<LecturaRegistradaDto>> EjecutarAsync(
        RegistrarLecturaSolicitud s, CancellationToken ct = default)
    {
        if (proveedorOrganizacion.OrganizacionId is not int organizacionId)
            return Resultado<LecturaRegistradaDto>.Falla("Su usuario no pertenece a ninguna ASADA; no puede registrar lecturas.");

        var errores = new List<string>();
        if (!ReglasPeriodo.EsMesValido(s.Anio, s.Mes)) errores.Add("El periodo indicado no es valido.");
        if (s.Lectura < 0) errores.Add("La lectura no puede ser negativa.");
        if (s.FechaLectura == default) errores.Add("La fecha de lectura es obligatoria.");
        else if (s.FechaLectura > fecha.Hoy) errores.Add("La fecha de lectura no puede ser futura.");
        Validacion.Largo(errores, s.Observaciones, 400, "Las observaciones");
        if (errores.Count > 0) return Resultado<LecturaRegistradaDto>.Falla(errores);

        var servicio = await repositorio.ObtenerServicioAsync(s.ServicioId, ct);
        if (servicio is null) return Resultado<LecturaRegistradaDto>.Falla("El servicio no existe.");
        if (servicio.Estado != EstadoRegistro.Activo)
            return Resultado<LecturaRegistradaDto>.Falla("El servicio esta inactivo; no se le pueden registrar lecturas.");

        var medidores = await repositorio.ListarMedidoresDeServicioAsync(s.ServicioId, ct);
        var medidorActual = medidores.FirstOrDefault(m => m.Estado == EstadoMedidor.Activo);
        if (medidorActual is null)
            return Resultado<LecturaRegistradaDto>.Falla("El servicio no tiene un medidor activo. Instale un medidor antes de registrar lecturas.");

        if (s.FechaLectura < medidorActual.FechaInstalacion)
            return Resultado<LecturaRegistradaDto>.Falla(
                $"La fecha de lectura es anterior a la instalacion del medidor ({medidorActual.FechaInstalacion:dd/MM/yyyy}).");

        var anterior = await repositorio.ObtenerUltimaLecturaAsync(s.ServicioId, ct);
        if (anterior is not null)
        {
            // Una sola lectura por servicio y periodo, y siempre en orden cronologico.
            if (ReglasPeriodo.Comparar(s.Anio, s.Mes, anterior.Anio, anterior.Mes) <= 0)
                return Resultado<LecturaRegistradaDto>.Falla(
                    $"Este servicio ya tiene una lectura del periodo {anterior.Mes:00}/{anterior.Anio} o de uno posterior.");

            if (s.FechaLectura < anterior.FechaLectura)
                return Resultado<LecturaRegistradaDto>.Falla(
                    $"La fecha de lectura es anterior a la lectura previa ({anterior.FechaLectura:dd/MM/yyyy}).");
        }

        // Cadena de medidores entre la lectura anterior y la actual (RB-013).
        IReadOnlyList<Medidor> cadena;
        if (anterior is null)
        {
            cadena = [medidorActual];
        }
        else
        {
            var indiceAnterior = medidores.ToList().FindIndex(m => m.Id == anterior.MedidorId);
            var indiceActual = medidores.ToList().FindIndex(m => m.Id == medidorActual.Id);
            if (indiceAnterior < 0 || indiceActual < indiceAnterior)
                return Resultado<LecturaRegistradaDto>.Falla("El historial de medidores del servicio es inconsistente.");

            cadena = medidores.Skip(indiceAnterior).Take(indiceActual - indiceAnterior + 1).ToList();
        }

        var consumo = CalculadoraConsumo.Calcular(anterior, cadena, s.Lectura);
        if (!consumo.EsValido) return Resultado<LecturaRegistradaDto>.Falla(consumo.Error!);

        // Motivos para revision manual.
        var motivos = new List<string>();

        if (!ReglasPeriodo.EstaEnVentana(s.Anio, s.Mes, s.FechaLectura))
            motivos.Add(
                $"Lectura fuera de la ventana del periodo ({ReglasPeriodo.InicioVentana(s.Anio, s.Mes):dd/MM} al {ReglasPeriodo.FinVentana(s.Anio, s.Mes):dd/MM/yyyy})");

        if (anterior is not null)
        {
            var (anioPrevio, mesPrevio) = ReglasPeriodo.Anterior(s.Anio, s.Mes);
            if (ReglasPeriodo.Comparar(anterior.Anio, anterior.Mes, anioPrevio, mesPrevio) < 0)
                motivos.Add("El periodo anterior quedo sin lectura; el consumo incluye mas de un mes");
        }

        var recientes = await repositorio.ObtenerConsumosRecientesAsync(
            s.ServicioId, ReglasConsumoAnormal.PeriodosReferencia, ct);
        if (ReglasConsumoAnormal.EsAnormal(consumo.Consumo, recientes))
            motivos.Add("Consumo muy superior al promedio de los ultimos periodos");

        var periodo = await repositorio.ObtenerOCrearPeriodoAsync(s.Anio, s.Mes, ct);

        var medicion = new Medicion
        {
            OrganizacionId = organizacionId,
            ServicioId = s.ServicioId,
            MedidorId = medidorActual.Id,
            PeriodoId = periodo.Id,
            FechaLectura = s.FechaLectura,
            Lectura = s.Lectura,
            Consumo = consumo.Consumo,
            RequiereRevision = motivos.Count > 0,
            MotivosRevision = motivos.Count > 0 ? string.Join(" | ", motivos) : null,
            Observaciones = Validacion.Limpiar(s.Observaciones),
        };

        await repositorio.AgregarMedicionAsync(medicion, ct);

        return Resultado<LecturaRegistradaDto>.Ok(new LecturaRegistradaDto(
            medicion.Id, medicion.Lectura, medicion.Consumo, medicion.RequiereRevision, motivos));
    }
}
