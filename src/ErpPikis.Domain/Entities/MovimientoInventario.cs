using ErpPikis.Domain.Enums;

namespace ErpPikis.Domain.Entities;

public class MovimientoInventario
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public TipoMovimientoInventario Tipo { get; set; }
    public int Cantidad { get; set; }
    public decimal? CostoUnitario { get; set; }
    public string? Motivo { get; set; }

    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }

    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
}
