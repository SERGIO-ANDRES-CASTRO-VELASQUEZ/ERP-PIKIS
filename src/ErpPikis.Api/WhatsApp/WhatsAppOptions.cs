using ErpPikis.Service.Alertas;

namespace ErpPikis.Api.WhatsApp;

public enum ProveedorWhatsApp
{
    Ninguno,
    CallMeBot,
    MetaCloud
}

public class WhatsAppOptions
{
    public const string Seccion = "WhatsApp";

    public ProveedorWhatsApp Proveedor { get; set; } = ProveedorWhatsApp.Ninguno;
    public int IntervaloEnvioSegundos { get; set; } = 30;
    public EnvioWhatsAppOptions Envio { get; set; } = new();
    public CallMeBotOptions CallMeBot { get; set; } = new();
    public MetaCloudOptions MetaCloud { get; set; } = new();
}

public class CallMeBotOptions
{
    public string Url { get; set; } = "https://api.callmebot.com/whatsapp.php";
}

public class MetaCloudOptions
{
    public string UrlBase { get; set; } = "https://graph.facebook.com";
    public string VersionApi { get; set; } = "v20.0";
    public string PhoneNumberId { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>Plantilla aprobada en Meta con un único parámetro en el cuerpo, ej. "Alerta de la tienda: {{1}}".</summary>
    public string Plantilla { get; set; } = "alerta_pikis";
    public string Idioma { get; set; } = "es";
}
