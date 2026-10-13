using System.Globalization;
using Asada.Aplicacion.Recibos;
using Asada.Aplicacion.Recibos.Dtos;
using Asada.Dominio.Enums;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Asada.Infraestructura.Documentos;

/// <summary>
/// Recibos en PDF con QuestPDF: formato media carta, dos recibos por hoja carta (se corta por la
/// linea punteada horizontal). Cada recibo tiene a la derecha un talon desprendible que se queda
/// la ASADA al cobrar.
///
/// Formato generico por ahora (decision del 13/10/2026): sin logo, colores ni texto de formas de
/// pago, a la espera de los datos de la ASADA. Todo el diseno esta en esta clase; si el cliente
/// pide cambios, se cambia aqui y nada mas.
///
/// Los textos van sin tildes, igual que el resto de la interfaz.
/// </summary>
public class GeneradorPdfRecibos : IGeneradorPdfRecibos
{
    // Hoja carta = 612 x 792 pt; cada recibo ocupa media hoja (612 x 396 pt). Se deja medio
    // punto de holgura para que el redondeo no empuje el segundo recibo a otra pagina.
    private const float AltoMediaCarta = 395.5f;
    private const float AnchoTalon = 150f;

    private static readonly string ColorTexto = Colors.Black;
    private static readonly string ColorSuave = Colors.Grey.Darken2;
    private static readonly string ColorLinea = Colors.Grey.Medium;
    private static readonly string ColorFondo = Colors.Grey.Lighten4;

    private static readonly string[] Meses =
    [
        "enero", "febrero", "marzo", "abril", "mayo", "junio",
        "julio", "agosto", "setiembre", "octubre", "noviembre", "diciembre",
    ];

    // Formato de Costa Rica fijo (no el de la maquina donde corre el servidor): coma decimal y
    // espacio duro como separador de miles, p. ej. "₡12 500,00".
    private static readonly NumberFormatInfo Numeros = new()
    {
        NumberDecimalSeparator = ",",
        NumberGroupSeparator = " ",
        NumberGroupSizes = [3],
    };

    static GeneradorPdfRecibos()
    {
        // Licencia Community: gratuita para organizaciones con ingresos brutos anuales menores a
        // US$1 millon (una ASADA califica). https://www.questpdf.com/license/
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Generar(IReadOnlyList<ReciboDto> recibos)
    {
        ArgumentNullException.ThrowIfNull(recibos);
        if (recibos.Count == 0) throw new ArgumentException("No hay recibos que imprimir.", nameof(recibos));

        return Document.Create(documento =>
        {
            for (var i = 0; i < recibos.Count; i += 2)
            {
                var primero = recibos[i];
                var segundo = i + 1 < recibos.Count ? recibos[i + 1] : null;

                documento.Page(pagina =>
                {
                    pagina.Size(PageSizes.Letter);
                    pagina.Margin(0);
                    pagina.DefaultTextStyle(t => t.FontSize(8).FontColor(ColorTexto));

                    pagina.Content().Column(col =>
                    {
                        col.Item().Height(AltoMediaCarta).Element(c => MediaCarta(c, primero));
                        col.Item().LineHorizontal(0.5f).LineColor(ColorLinea).LineDashPattern([4f, 3f]);
                        if (segundo is not null)
                            col.Item().Height(AltoMediaCarta).Element(c => MediaCarta(c, segundo));
                    });
                });
            }
        })
        .WithMetadata(new DocumentMetadata
        {
            Title = recibos.Count == 1 ? $"Recibo {recibos[0].Numero}" : $"Recibos {Periodo(recibos[0].Anio, recibos[0].Mes)}",
            Author = recibos[0].OrganizacionNombre,
            Creator = "Sistema de Gestion ASADA",
        })
        .GeneratePdf();
    }

    /// <summary>Media hoja: el recibo a la izquierda y el talon a la derecha.</summary>
    private static void MediaCarta(IContainer contenedor, ReciboDto r)
    {
        // ScaleToFit: si algun recibo trae un desglose mas largo de lo normal (muchos tramos y
        // nombres largos), se reduce un poco en vez de partirse en dos paginas.
        contenedor.PaddingVertical(22).PaddingHorizontal(26).ScaleToFit().Row(fila =>
        {
            fila.RelativeItem().PaddingRight(14).Element(c => CuerpoRecibo(c, r));
            fila.ConstantItem(1).LineVertical(0.5f).LineColor(ColorLinea).LineDashPattern([4f, 3f]);
            fila.ConstantItem(AnchoTalon).PaddingLeft(12).Element(c => Talon(c, r));
        });
    }

    private static void CuerpoRecibo(IContainer contenedor, ReciboDto r)
    {
        contenedor.Column(col =>
        {
            col.Spacing(7);

            // Encabezado: ASADA emisora y datos del recibo.
            col.Item().Row(fila =>
            {
                fila.RelativeItem().Column(asada =>
                {
                    asada.Item().Text(r.OrganizacionNombre).FontSize(12).Bold();
                    var contacto = new List<string>();
                    if (!string.IsNullOrWhiteSpace(r.OrganizacionCedulaJuridica)) contacto.Add($"Ced. juridica {r.OrganizacionCedulaJuridica}");
                    if (!string.IsNullOrWhiteSpace(r.OrganizacionTelefono)) contacto.Add($"Tel. {r.OrganizacionTelefono}");
                    if (contacto.Count > 0) asada.Item().Text(string.Join("  ·  ", contacto)).FontColor(ColorSuave);
                    if (!string.IsNullOrWhiteSpace(r.OrganizacionCorreo)) asada.Item().Text(r.OrganizacionCorreo).FontColor(ColorSuave);
                });
                fila.ConstantItem(150).AlignRight().Column(datos =>
                {
                    datos.Item().AlignRight().Text("RECIBO DE AGUA").FontSize(7).Bold().FontColor(ColorSuave).LetterSpacing(0.05f);
                    datos.Item().AlignRight().Text(r.Numero).FontSize(10).Bold();
                    datos.Item().AlignRight().Text($"Periodo: {Periodo(r.Anio, r.Mes)}");
                    if (r.Estado == EstadoRecibo.Anulado)
                        datos.Item().AlignRight().Text("ANULADO").FontSize(10).Bold().FontColor(Colors.Red.Darken2);
                });
            });

            col.Item().LineHorizontal(0.75f).LineColor(ColorTexto);

            // Abonado y medicion.
            col.Item().Row(fila =>
            {
                fila.RelativeItem(11).PaddingRight(10).Column(abonado =>
                {
                    abonado.Item().Text(r.AbonadoNombre).FontSize(9.5f).Bold();
                    abonado.Item().Text($"Abonado {r.AbonadoCodigo}  ·  Servicio #{r.ServicioId}");
                    abonado.Item().Text(r.PropiedadDireccion);
                    abonado.Item().Text($"Categoria: {r.CategoriaNombre}");
                });
                fila.RelativeItem(9).Background(ColorFondo).Padding(6).Column(medicion =>
                {
                    Dato(medicion, "Medidor", r.NumeroSerieMedidor);
                    Dato(medicion, "Lectura anterior", r.LecturaAnterior is decimal la ? Metros(la) : "—");
                    Dato(medicion, $"Lectura actual ({Fecha(r.FechaLectura)})", Metros(r.LecturaActual));
                    medicion.Item().PaddingTop(2).Row(f =>
                    {
                        f.RelativeItem().Text("Consumo del periodo").Bold();
                        f.AutoItem().Text(Metros(r.Consumo)).Bold();
                    });
                    if (r.HuboCambioMedidor)
                        medicion.Item().Text("Incluye cambio de medidor en el periodo.").FontSize(7).Italic().FontColor(ColorSuave);
                });
            });

            // Desglose del cobro.
            col.Item().Table(tabla =>
            {
                tabla.ColumnsDefinition(c =>
                {
                    c.RelativeColumn();
                    c.ConstantColumn(44);
                    c.ConstantColumn(56);
                    c.ConstantColumn(68);
                });

                tabla.Header(h =>
                {
                    EncabezadoTabla(h.Cell(), "Concepto", alinearDerecha: false);
                    EncabezadoTabla(h.Cell(), "m³", alinearDerecha: true);
                    EncabezadoTabla(h.Cell(), "Precio", alinearDerecha: true);
                    EncabezadoTabla(h.Cell(), "Monto", alinearDerecha: true);
                });

                foreach (var l in r.Lineas)
                {
                    CeldaTabla(tabla.Cell()).Text(l.Concepto);
                    CeldaTabla(tabla.Cell()).AlignRight().Text(l.Cantidad is decimal c ? Cantidad(c) : "");
                    CeldaTabla(tabla.Cell()).AlignRight().Text(l.PrecioUnitario is decimal p ? Precio(p) : "");
                    CeldaTabla(tabla.Cell()).AlignRight().Text(Colones(l.Monto));
                }

                if (r.MontoHidrante > 0)
                {
                    tabla.Cell().ColumnSpan(3).PaddingTop(3).Text("Subtotal agua");
                    tabla.Cell().PaddingTop(3).AlignRight().Text(Colones(r.SubtotalAgua));
                    tabla.Cell().ColumnSpan(3).Text("Hidrantes");
                    tabla.Cell().AlignRight().Text(Colones(r.MontoHidrante));
                }
            });

            // Total y vencimiento.
            col.Item().Background(ColorFondo).PaddingVertical(5).PaddingHorizontal(6).Row(fila =>
            {
                fila.RelativeItem().Column(fechas =>
                {
                    fechas.Item().Text($"Emitido: {Fecha(r.FechaEmision)}");
                    fechas.Item().Text($"Vence: {Fecha(r.FechaVencimiento)}").Bold();
                });
                fila.AutoItem().AlignMiddle().Text(t =>
                {
                    t.Span("Total a pagar  ").FontSize(9);
                    t.Span(Colones(r.Total)).FontSize(13).Bold();
                });
            });

            col.Item().Text($"Pague antes del {Fecha(r.FechaVencimiento)}. Presente este recibo completo al pagar.")
                .FontSize(7).FontColor(ColorSuave);
        });
    }

    /// <summary>Parte desprendible que se queda la ASADA como constancia del cobro.</summary>
    private static void Talon(IContainer contenedor, ReciboDto r)
    {
        contenedor.Column(col =>
        {
            col.Spacing(4);
            col.Item().Text("TALON PARA LA ASADA").FontSize(7).Bold().FontColor(ColorSuave).LetterSpacing(0.05f);
            col.Item().Text(r.OrganizacionNombre).Bold();
            col.Item().PaddingTop(2).LineHorizontal(0.75f).LineColor(ColorTexto);

            Dato(col, "Recibo", r.Numero);
            Dato(col, "Periodo", Periodo(r.Anio, r.Mes));
            Dato(col, "Abonado", r.AbonadoCodigo);
            col.Item().Text(r.AbonadoNombre).Bold();
            Dato(col, "Servicio", $"#{r.ServicioId}");
            Dato(col, "Consumo", Metros(r.Consumo));
            Dato(col, "Vence", Fecha(r.FechaVencimiento));
            if (r.Estado == EstadoRecibo.Anulado)
                col.Item().Text("ANULADO").Bold().FontColor(Colors.Red.Darken2);

            col.Item().PaddingTop(4).Background(ColorFondo).Padding(5).Column(total =>
            {
                total.Item().Text("Total a pagar").FontSize(7);
                total.Item().AlignRight().Text(Colones(r.Total)).FontSize(12).Bold();
            });

            col.Item().PaddingTop(12).Text("Fecha de pago:").FontSize(7);
            col.Item().PaddingTop(10).LineHorizontal(0.5f).LineColor(ColorLinea);
            col.Item().PaddingTop(6).Text("Recibido por:").FontSize(7);
            col.Item().PaddingTop(10).LineHorizontal(0.5f).LineColor(ColorLinea);
        });
    }

    private static void Dato(ColumnDescriptor col, string etiqueta, string valor)
        => col.Item().Row(f =>
        {
            f.RelativeItem().Text(etiqueta).FontColor(ColorSuave);
            f.AutoItem().Text(valor);
        });

    private static void EncabezadoTabla(IContainer celda, string texto, bool alinearDerecha)
    {
        var c = celda.BorderBottom(0.75f).BorderColor(ColorTexto).PaddingBottom(2);
        (alinearDerecha ? c.AlignRight() : c).Text(texto).Bold();
    }

    private static IContainer CeldaTabla(IContainer celda)
        => celda.BorderBottom(0.25f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2);

    // --- Formatos (iguales a los de la pantalla, pero con el formato numerico de Costa Rica fijo) ---

    private static string Periodo(int anio, int mes) => $"{(mes is >= 1 and <= 12 ? Meses[mes - 1] : mes.ToString())} {anio}";

    private static string Fecha(DateOnly fecha) => fecha.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    private static string Metros(decimal valor) => $"{Cantidad(valor)} m³";

    private static string Cantidad(decimal valor) => valor.ToString("#,##0.###", Numeros);

    private static string Colones(decimal monto) => "₡" + monto.ToString("#,##0.00", Numeros);

    /// <summary>Hasta 4 decimales, sin ceros de mas (₡312,5 -> ₡312,50; ₡12,3456 se respeta).</summary>
    private static string Precio(decimal precio)
        => decimal.Round(precio, 2) == precio ? Colones(precio) : "₡" + precio.ToString("#,##0.00##", Numeros);
}
