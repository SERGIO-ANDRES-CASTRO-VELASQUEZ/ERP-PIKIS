using ErpPikis.Domain.Enums;

namespace ErpPikis.Domain.Entities;

public class NotificacionWhatsApp
{
    public int Id { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public EstadoNotificacion Estado { get; private set; } = EstadoNotificacion.Pendiente;
    public int Intentos { get; private set; }
    public string? UltimoError { get; private set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime ProximoIntento { get; private set; } = DateTime.UtcNow;
    public DateTime? FechaEnvio { get; private set; }

    public int? AlertaId { get; set; }
    public Alerta? Alerta { get; set; }

    public int DestinatarioId { get; set; }
    public DestinatarioAlerta? Destinatario { get; set; }

    public void MarcarEnviada()
    {
        Intentos++;
        Estado = EstadoNotificacion.Enviada;
        FechaEnvio = DateTime.UtcNow;
        UltimoError = null;
    }

    public void RegistrarFallo(string error, int maxIntentos)
    {
        Intentos++;
        UltimoError = error.Length > 500 ? error[..500] : error;

        if (Intentos >= maxIntentos)
        {
            Estado = EstadoNotificacion.Fallida;
            return;
        }

        var espera = TimeSpan.FromMinutes(Math.Min(Math.Pow(2, Intentos - 1), 60));
        ProximoIntento = DateTime.UtcNow.Add(espera);
    }
}
