namespace ErpPikis.Domain.Entities;

public class DestinatarioAlerta
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Número internacional solo con dígitos, ej. 573001234567.</summary>
    public string Telefono { get; set; } = string.Empty;

    /// <summary>Clave personal del número en CallMeBot. No aplica para la API de Meta.</summary>
    public string? ApiKey { get; set; }

    public bool Activo { get; set; } = true;
}
