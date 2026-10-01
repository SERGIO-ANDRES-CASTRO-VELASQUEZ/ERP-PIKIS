using ErpPikis.Service.Alertas;

namespace ErpPikis.Api.WhatsApp;

/// <summary>
/// CallMeBot: gratuito para uso personal. Cada número debe autorizar el bot una vez
/// (enviar "I allow callmebot to send me messages" al número del bot) y usar la apikey que recibe.
/// </summary>
public class CallMeBotSender : IWhatsAppSender
{
    private readonly HttpClient _http;
    private readonly CallMeBotOptions _opciones;

    public CallMeBotSender(HttpClient http, WhatsAppOptions opciones)
    {
        _http = http;
        _opciones = opciones.CallMeBot;
    }

    public bool Habilitado => true;
    public string Proveedor => "CallMeBot";

    public async Task EnviarAsync(string telefono, string? apiKey, string mensaje, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new WhatsAppException($"El número {telefono} no tiene ApiKey de CallMeBot.");

        var url = $"{_opciones.Url}?phone=%2B{telefono}&apikey={Uri.EscapeDataString(apiKey)}&text={Uri.EscapeDataString(mensaje)}";

        try
        {
            using var respuesta = await _http.GetAsync(url, ct);
            var cuerpo = await respuesta.Content.ReadAsStringAsync(ct);

            if (!respuesta.IsSuccessStatusCode
                || cuerpo.Contains("invalid", StringComparison.OrdinalIgnoreCase)
                || cuerpo.Contains("error", StringComparison.OrdinalIgnoreCase))
                throw new WhatsAppException($"CallMeBot respondió {(int)respuesta.StatusCode}: {Resumir(cuerpo)}");
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            throw new WhatsAppException($"Sin conexión con CallMeBot: {e.Message}", e);
        }
    }

    private static string Resumir(string html)
    {
        var texto = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", " ").Trim();
        return texto.Length > 200 ? texto[..200] : texto;
    }
}
