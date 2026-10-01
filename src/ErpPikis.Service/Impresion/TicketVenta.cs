using System.Globalization;

namespace ErpPikis.Service.Impresion;

public record LineaTicket(string Producto, int Cantidad, decimal PrecioUnitario, decimal Subtotal);

public record TicketVenta(
    TiendaOptions Tienda,
    int Numero,
    DateTime FechaLocal,
    string Cajero,
    string TipoPago,
    bool EsCredito,
    string? Cliente,
    decimal? SaldoCliente,
    IReadOnlyList<LineaTicket> Lineas,
    decimal Total,
    decimal? MontoRecibido,
    decimal? Cambio,
    bool EsReimpresion);

internal static class FormatoTicket
{
    private static readonly NumberFormatInfo Pesos = new()
    {
        NumberGroupSeparator = ".",
        NumberDecimalSeparator = ",",
        NumberDecimalDigits = 0
    };

    public static string Dinero(decimal valor) => "$" + valor.ToString("N0", Pesos);

    public static string Fecha(DateTime fecha) => fecha.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);

    public static string Numero(int numero) => numero.ToString("D6", CultureInfo.InvariantCulture);

    public static string NombrePago(string tipoPago) => tipoPago switch
    {
        "Credito" => "Crédito (fiado)",
        _ => tipoPago
    };
}
