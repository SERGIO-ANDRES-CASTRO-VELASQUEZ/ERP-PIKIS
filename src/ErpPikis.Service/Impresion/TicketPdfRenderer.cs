using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ErpPikis.Service.Impresion;

public static class TicketPdfRenderer
{
    public static byte[] Generar(TicketVenta t, int anchoPapelMm)
    {
        var angosto = anchoPapelMm < 80;
        var margenMm = angosto ? 2.5f : 4f;
        var fuente = angosto ? 7.5f : 8.5f;

        return Document.Create(doc => doc.Page(page =>
        {
            page.ContinuousSize(anchoPapelMm, Unit.Millimetre);
            page.Margin(margenMm, Unit.Millimetre);
            page.PageColor(Colors.White);
            page.DefaultTextStyle(x => x.FontSize(fuente).FontColor(Colors.Black));

            page.Content().Column(col =>
            {
                col.Spacing(1.5f);

                col.Item().AlignCenter().Text(t.Tienda.Nombre).Bold().FontSize(fuente + 4);
                if (!string.IsNullOrWhiteSpace(t.Tienda.Nit))
                    col.Item().AlignCenter().Text($"NIT {t.Tienda.Nit}");
                if (!string.IsNullOrWhiteSpace(t.Tienda.Direccion))
                    col.Item().AlignCenter().Text(t.Tienda.Direccion);
                if (!string.IsNullOrWhiteSpace(t.Tienda.Telefono))
                    col.Item().AlignCenter().Text($"Tel. {t.Tienda.Telefono}");

                Separador(col);

                col.Item().Text(txt =>
                {
                    txt.Span("Tiquete N° ").Bold();
                    txt.Span(FormatoTicket.Numero(t.Numero));
                });
                if (t.EsReimpresion)
                    col.Item().AlignCenter().Text("*** REIMPRESIÓN ***").Bold();
                col.Item().Text($"Fecha: {FormatoTicket.Fecha(t.FechaLocal)}");
                col.Item().Text($"Cajero: {t.Cajero}");
                if (t.Cliente is not null)
                    col.Item().Text($"Cliente: {t.Cliente}");

                Separador(col);

                foreach (var linea in t.Lineas)
                {
                    col.Item().Text(linea.Producto);
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"  {linea.Cantidad} x {FormatoTicket.Dinero(linea.PrecioUnitario)}");
                        row.AutoItem().Text(FormatoTicket.Dinero(linea.Subtotal));
                    });
                }

                Separador(col);

                col.Item().Row(row =>
                {
                    row.RelativeItem().Text("TOTAL").Bold().FontSize(fuente + 3);
                    row.AutoItem().Text(FormatoTicket.Dinero(t.Total)).Bold().FontSize(fuente + 3);
                });
                Fila(col, "Forma de pago", FormatoTicket.NombrePago(t.TipoPago));
                if (t.MontoRecibido is { } recibido)
                    Fila(col, "Recibido", FormatoTicket.Dinero(recibido));
                if (t.Cambio is { } cambio)
                    Fila(col, "Cambio", FormatoTicket.Dinero(cambio));

                if (t.EsCredito)
                {
                    if (t.SaldoCliente is { } saldo)
                        Fila(col, "Saldo pendiente", FormatoTicket.Dinero(saldo));
                    col.Item().PaddingTop(14).LineHorizontal(0.5f);
                    col.Item().AlignCenter().Text("Firma del cliente");
                }

                Separador(col);
                col.Item().AlignCenter().Text(t.Tienda.MensajePie).Italic();
                col.Item().AlignCenter().Text("Documento no válido como factura electrónica").FontSize(fuente - 1.5f);
            });
        })).GeneratePdf();
    }

    private static void Separador(ColumnDescriptor col) =>
        col.Item().PaddingVertical(2).LineHorizontal(0.5f).LineColor(Colors.Black);

    private static void Fila(ColumnDescriptor col, string etiqueta, string valor) =>
        col.Item().Row(row =>
        {
            row.RelativeItem().Text(etiqueta);
            row.AutoItem().Text(valor);
        });
}
