namespace ErpPikis.Domain.Entities;

public class DetalleVenta
{
    public int Id { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal CostoUnitario { get; set; }
    public decimal Subtotal { get; set; }

    public decimal Utilidad => Subtotal - CostoUnitario * Cantidad;

    public int VentaId { get; set; }
    public Venta? Venta { get; set; }

    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }
}
