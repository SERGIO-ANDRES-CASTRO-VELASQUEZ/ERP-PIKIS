using System.Text;

namespace ErpPikis.Service.Impresion;

public enum Alineacion : byte
{
    Izquierda = 0,
    Centro = 1,
    Derecha = 2
}

public sealed class EscPosBuilder
{
    private const byte Esc = 0x1B;
    private const byte Gs = 0x1D;

    private static readonly Dictionary<int, byte> TablasCodigos = new()
    {
        [437] = 0,
        [850] = 2,
        [858] = 19,
        [1252] = 16
    };

    private readonly MemoryStream _buffer = new();
    private readonly Encoding _encoding;

    static EscPosBuilder() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    public EscPosBuilder(int codePage, int anchoCaracteres)
    {
        if (!TablasCodigos.TryGetValue(codePage, out var tabla))
            throw new ArgumentException($"Página de códigos no soportada: {codePage}. Use 437, 850, 858 o 1252.");

        _encoding = Encoding.GetEncoding(codePage);
        Ancho = anchoCaracteres;
        Comando(Esc, (byte)'@');
        Comando(Esc, (byte)'t', tabla);
    }

    public int Ancho { get; }

    public EscPosBuilder Alinear(Alineacion alineacion) => Comando(Esc, (byte)'a', (byte)alineacion);

    public EscPosBuilder Negrita(bool activa) => Comando(Esc, (byte)'E', activa ? (byte)1 : (byte)0);

    public EscPosBuilder TamanoDoble(bool ancho, bool alto) =>
        Comando(Gs, (byte)'!', (byte)((ancho ? 0x10 : 0) | (alto ? 0x01 : 0)));

    public EscPosBuilder Linea(string texto = "")
    {
        var bytes = _encoding.GetBytes(texto + "\n");
        _buffer.Write(bytes, 0, bytes.Length);
        return this;
    }

    public EscPosBuilder LineasAjustadas(string texto, int? ancho = null)
    {
        foreach (var parte in Ajustar(texto, ancho ?? Ancho))
            Linea(parte);
        return this;
    }

    public EscPosBuilder Columnas(string izquierda, string derecha, int? ancho = null)
    {
        var total = ancho ?? Ancho;
        var maxIzquierda = Math.Max(1, total - derecha.Length - 1);
        if (izquierda.Length > maxIzquierda)
            izquierda = izquierda[..maxIzquierda];
        return Linea(izquierda + new string(' ', total - izquierda.Length - derecha.Length) + derecha);
    }

    public EscPosBuilder Separador(char caracter = '-') => Linea(new string(caracter, Ancho));

    public EscPosBuilder Avanzar(byte lineas) => Comando(Esc, (byte)'d', lineas);

    public EscPosBuilder CortarParcial() => Comando(Gs, (byte)'V', 66, 0);

    /// <summary>Pulso al conector RJ11 del cajón monedero (pin 2).</summary>
    public EscPosBuilder AbrirCajon() => Comando(Esc, (byte)'p', 0, 25, 250);

    public byte[] Construir() => _buffer.ToArray();

    private EscPosBuilder Comando(params byte[] bytes)
    {
        _buffer.Write(bytes, 0, bytes.Length);
        return this;
    }

    private static IEnumerable<string> Ajustar(string texto, int ancho)
    {
        var actual = new StringBuilder();
        foreach (var palabra in texto.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var resto = palabra;
            while (resto.Length > ancho)
            {
                if (actual.Length > 0)
                {
                    yield return actual.ToString();
                    actual.Clear();
                }
                yield return resto[..ancho];
                resto = resto[ancho..];
            }

            if (actual.Length > 0 && actual.Length + 1 + resto.Length > ancho)
            {
                yield return actual.ToString();
                actual.Clear();
            }

            if (actual.Length > 0)
                actual.Append(' ');
            actual.Append(resto);
        }

        if (actual.Length > 0)
            yield return actual.ToString();
    }
}

public static class TicketEscPosRenderer
{
    public static byte[] Generar(TicketVenta t, ImpresoraOptions opciones, bool abrirCajon)
    {
        var p = new EscPosBuilder(opciones.CodePage, opciones.AnchoEnCaracteres());

        if (abrirCajon)
            p.AbrirCajon();

        p.Alinear(Alineacion.Centro)
            .Negrita(true).TamanoDoble(true, true)
            .LineasAjustadas(t.Tienda.Nombre, p.Ancho / 2)
            .TamanoDoble(false, false).Negrita(false);

        if (!string.IsNullOrWhiteSpace(t.Tienda.Nit))
            p.Linea($"NIT {t.Tienda.Nit}");
        if (!string.IsNullOrWhiteSpace(t.Tienda.Direccion))
            p.LineasAjustadas(t.Tienda.Direccion);
        if (!string.IsNullOrWhiteSpace(t.Tienda.Telefono))
            p.Linea($"Tel. {t.Tienda.Telefono}");

        p.Alinear(Alineacion.Izquierda).Separador();

        p.Negrita(true).Linea($"Tiquete N° {FormatoTicket.Numero(t.Numero)}").Negrita(false);
        if (t.EsReimpresion)
            p.Alinear(Alineacion.Centro).Negrita(true).Linea("*** REIMPRESION ***").Negrita(false).Alinear(Alineacion.Izquierda);
        p.Linea($"Fecha: {FormatoTicket.Fecha(t.FechaLocal)}");
        p.LineasAjustadas($"Cajero: {t.Cajero}");
        if (t.Cliente is not null)
            p.LineasAjustadas($"Cliente: {t.Cliente}");

        p.Separador();

        foreach (var linea in t.Lineas)
        {
            p.LineasAjustadas(linea.Producto);
            p.Columnas($"  {linea.Cantidad} x {FormatoTicket.Dinero(linea.PrecioUnitario)}", FormatoTicket.Dinero(linea.Subtotal));
        }

        p.Separador();

        p.Negrita(true).TamanoDoble(false, true)
            .Columnas("TOTAL", FormatoTicket.Dinero(t.Total))
            .TamanoDoble(false, false).Negrita(false);
        p.Columnas("Forma de pago", FormatoTicket.NombrePago(t.TipoPago));
        if (t.MontoRecibido is { } recibido)
            p.Columnas("Recibido", FormatoTicket.Dinero(recibido));
        if (t.Cambio is { } cambio)
            p.Columnas("Cambio", FormatoTicket.Dinero(cambio));

        if (t.EsCredito)
        {
            if (t.SaldoCliente is { } saldo)
                p.Negrita(true).Columnas("Saldo pendiente", FormatoTicket.Dinero(saldo)).Negrita(false);
            p.Linea().Linea().Linea(new string('_', p.Ancho)).Alinear(Alineacion.Centro).Linea("Firma del cliente").Alinear(Alineacion.Izquierda);
        }

        p.Separador()
            .Alinear(Alineacion.Centro)
            .LineasAjustadas(t.Tienda.MensajePie)
            .Linea("No valido como factura electronica")
            .Alinear(Alineacion.Izquierda)
            .Avanzar(4);

        if (opciones.CortarPapel)
            p.CortarParcial();

        return p.Construir();
    }
}
