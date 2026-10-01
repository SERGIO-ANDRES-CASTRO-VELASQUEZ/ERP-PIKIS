using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using ErpPikis.Service.Alertas;

namespace ErpPikis.Api.WhatsApp;

/// <summary>
/// API oficial de WhatsApp Business (Meta). Los mensajes que inicia el negocio deben usar una plantilla aprobada.
/// </summary>
public partial class MetaCloudSender : IWhatsAppSender
{
    private readonly HttpClient _http;
    private readonly MetaCloudOptions _opciones;

    public MetaCloudSender(HttpClient http, WhatsAppOptions opciones)
    {
        _http = http;
        _opciones = opciones.MetaCloud;
        if (string.IsNullOrWhiteSpace(_opciones.PhoneNumberId) || string.IsNullOrWhiteSpace(_opciones.AccessToken))
            throw new InvalidOperationException("WhatsApp:MetaCloud requiere PhoneNumberId y AccessToken.");
    }

    public bool Habilitado => true;
    public string Proveedor => "MetaCloud";

    public async Task EnviarAsync(string telefono, string? apiKey, string mensaje, CancellationToken ct = default)
    {
        var cuerpo = new
        {
            messaging_product = "whatsapp",
            to = telefono,
            type = "template",
            template = new
            {
                name = _opciones.Plantilla,
                language = new { code = _opciones.Idioma },
                components = new object[]
                {
                    new
                    {
                        type = "body",
                        parameters = new object[] { new { type = "text", text = ParaPlantilla(mensaje) } }
                    }
                }
            }
        };

        using var solicitud = new HttpRequestMessage(HttpMethod.Post,
            $"{_opciones.UrlBase.TrimEnd('/')}/{_opciones.VersionApi}/{_opciones.PhoneNumberId}/messages")
        {
            Content = JsonContent.Create(cuerpo)
        };
        solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opciones.AccessToken);

        try
        {
            using var respuesta = await _http.SendAsync(solicitud, ct);
            if (!respuesta.IsSuccessStatusCode)
            {
                var error = await respuesta.Content.ReadAsStringAsync(ct);
                throw new WhatsAppException($"Meta respondió {(int)respuesta.StatusCode}: {(error.Length > 300 ? error[..300] : error)}");
            }
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            throw new WhatsAppException($"Sin conexión con WhatsApp Cloud API: {e.Message}", e);
        }
    }

    /// <summary>Meta no acepta saltos de línea, tabulaciones ni más de 4 espacios seguidos en los parámetros.</summary>
    private static string ParaPlantilla(string mensaje)
    {
        var texto = EspaciosLargos().Replace(mensaje.Replace("\r", "").Replace("\n", " | ").Replace("\t", " "), "   ");
        return texto.Length > 1000 ? texto[..1000] : texto;
    }

    [GeneratedRegex(" {4,}")]
    private static partial Regex EspaciosLargos();
}
