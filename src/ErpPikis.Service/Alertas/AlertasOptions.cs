using ErpPikis.Domain.Enums;

namespace ErpPikis.Service.Alertas;

public class AlertasOptions
{
    public const string Seccion = "Alertas";

    public int IntervaloRevisionMinutos { get; set; } = 30;

    /// <summary>Alerta cuando la deuda del vecino llega a este % de su límite.</summary>
    public int PorcentajeLimiteCredito { get; set; } = 80;

    public int DiasSinAbonar { get; set; } = 30;

    /// <summary>Diferencia de caja (en pesos) a partir de la cual se alerta el cierre.</summary>
    public decimal UmbralDescuadreCaja { get; set; } = 2000;

    public int HorasTurnoAbierto { get; set; } = 14;

    public decimal MargenMinimoPorcentaje { get; set; } = 10;

    public bool ResumenDiarioHabilitado { get; set; } = true;

    /// <summary>Hora local (HH:mm) desde la que se envía el resumen del día.</summary>
    public string ResumenDiarioHora { get; set; } = "21:00";

    /// <summary>Severidad mínima que se envía por WhatsApp. El resumen diario se envía siempre.</summary>
    public SeveridadAlerta SeveridadMinimaWhatsApp { get; set; } = SeveridadAlerta.Advertencia;

    public string PrefijoPais { get; set; } = "57";
}

public class EnvioWhatsAppOptions
{
    public int MaxIntentos { get; set; } = 10;
    public int PausaEntreMensajesSegundos { get; set; } = 3;
    public int LoteMaximo { get; set; } = 20;
}
