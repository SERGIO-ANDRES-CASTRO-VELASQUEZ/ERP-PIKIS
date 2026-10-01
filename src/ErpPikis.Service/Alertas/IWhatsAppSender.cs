namespace ErpPikis.Service.Alertas;

public interface IWhatsAppSender
{
    bool Habilitado { get; }
    string Proveedor { get; }

    /// <summary>Lanza <see cref="WhatsAppException"/> si el proveedor rechaza o no responde.</summary>
    Task EnviarAsync(string telefono, string? apiKey, string mensaje, CancellationToken ct = default);
}

public class WhatsAppException : Exception
{
    public WhatsAppException(string message, Exception? inner = null) : base(message, inner)
    {
    }
}

public class WhatsAppDeshabilitado : IWhatsAppSender
{
    public bool Habilitado => false;
    public string Proveedor => "Ninguno";

    public Task EnviarAsync(string telefono, string? apiKey, string mensaje, CancellationToken ct = default) =>
        throw new WhatsAppException("WhatsApp no está configurado (WhatsApp:Proveedor = Ninguno).");
}
