namespace ErpPikis.Service.Impresion;

public class TiendaOptions
{
    public const string Seccion = "Tienda";

    public string Nombre { get; set; } = "Tienda PIKIS";
    public string? Nit { get; set; }
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
    public string MensajePie { get; set; } = "¡Gracias por su compra!";
}

public enum ModoImpresora
{
    Ninguna,
    Red,
    Windows
}

public class ImpresoraOptions
{
    public const string Seccion = "ImpresoraTermica";

    public ModoImpresora Modo { get; set; } = ModoImpresora.Ninguna;

    /// <summary>IP de la impresora cuando Modo = Red (puerto RAW 9100).</summary>
    public string? Host { get; set; }
    public int Puerto { get; set; } = 9100;

    /// <summary>Nombre exacto de la impresora instalada en Windows cuando Modo = Windows.</summary>
    public string? NombreImpresora { get; set; }

    /// <summary>58 u 80.</summary>
    public int AnchoPapelMm { get; set; } = 80;

    /// <summary>0 = automático (48 para 80 mm, 32 para 58 mm, fuente A).</summary>
    public int CaracteresPorLinea { get; set; }

    /// <summary>Página de códigos de la impresora: 850, 858, 437 o 1252.</summary>
    public int CodePage { get; set; } = 850;

    public bool AbrirCajonEnVentaEfectivo { get; set; } = true;
    public bool CortarPapel { get; set; } = true;
    public int TimeoutSegundos { get; set; } = 5;

    public int AnchoEnCaracteres() =>
        CaracteresPorLinea > 0 ? CaracteresPorLinea : AnchoPapelMm >= 80 ? 48 : 32;
}
