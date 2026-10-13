using Asada.Aplicacion.Recibos.Dtos;

namespace Asada.Aplicacion.Recibos;

/// <summary>
/// Convierte recibos ya emitidos en un documento PDF listo para imprimir. La implementacion
/// (con QuestPDF) vive en Asada.Infraestructura; aqui solo queda el contrato, para que los casos
/// de uso no dependan de la libreria de PDF y se puedan probar sin ella.
///
/// Todo el diseno del recibo (formato media carta, talon, encabezado) es responsabilidad de la
/// implementacion: si el cliente pide otro formato, cambia esa clase y nada mas.
/// </summary>
public interface IGeneradorPdfRecibos
{
    /// <summary>Un solo PDF con todos los recibos, en el orden recibido.</summary>
    byte[] Generar(IReadOnlyList<ReciboDto> recibos);
}
