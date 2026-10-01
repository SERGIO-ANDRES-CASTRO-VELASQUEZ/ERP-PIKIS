using ErpPikis.Domain.Enums;

namespace ErpPikis.Domain.Entities;

public class Alerta
{
    public int Id { get; set; }
    public TipoAlerta Tipo { get; set; }
    public SeveridadAlerta Severidad { get; set; }

    /// <summary>Identifica la condición (ej. "stock-bajo:12") para no repetir la misma alerta.</summary>
    public string Clave { get; set; } = string.Empty;

    public string Titulo { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    public bool Leida { get; private set; }
    public DateTime? FechaLeida { get; private set; }

    /// <summary>True mientras la condición siga presente. Las alertas de evento nacen inactivas.</summary>
    public bool Activa { get; set; } = true;
    public DateTime? FechaResolucion { get; private set; }

    public void MarcarLeida()
    {
        if (Leida)
            return;
        Leida = true;
        FechaLeida = DateTime.UtcNow;
    }

    public void Resolver()
    {
        if (!Activa)
            return;
        Activa = false;
        FechaResolucion = DateTime.UtcNow;
    }
}
