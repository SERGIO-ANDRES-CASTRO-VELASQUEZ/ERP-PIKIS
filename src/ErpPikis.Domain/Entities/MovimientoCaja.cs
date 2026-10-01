using ErpPikis.Domain.Enums;

namespace ErpPikis.Domain.Entities;

public class MovimientoCaja
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public TipoMovimientoCaja Tipo { get; set; }
    public decimal Monto { get; set; }
    public string Descripcion { get; set; } = string.Empty;

    public int TurnoCajaId { get; set; }
    public TurnoCaja? TurnoCaja { get; set; }

    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    public int? ClienteId { get; set; }
    public Cliente? Cliente { get; set; }
}
