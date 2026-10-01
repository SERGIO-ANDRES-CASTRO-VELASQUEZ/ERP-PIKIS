using ErpPikis.Domain.Enums;
using ErpPikis.Domain.Exceptions;

namespace ErpPikis.Domain.Entities;

public class Venta
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public TipoPago TipoPago { get; set; }
    public decimal Total { get; private set; }
    public decimal? MontoRecibido { get; set; }

    public decimal? Cambio => MontoRecibido is null ? null : MontoRecibido - Total;

    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    public int TurnoCajaId { get; set; }
    public TurnoCaja? TurnoCaja { get; set; }

    public int? ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    public ICollection<DetalleVenta> Detalles { get; set; } = new List<DetalleVenta>();

    public void AgregarProducto(Producto producto, int cantidad)
    {
        producto.DescontarStock(cantidad);

        var subtotal = producto.PrecioVenta * cantidad;
        Detalles.Add(new DetalleVenta
        {
            Producto = producto,
            ProductoId = producto.Id,
            Cantidad = cantidad,
            PrecioUnitario = producto.PrecioVenta,
            CostoUnitario = producto.PrecioCompra,
            Subtotal = subtotal
        });
        Total += subtotal;
    }

    public void ValidarPago()
    {
        if (Detalles.Count == 0)
            throw new DomainException("La venta no tiene productos.");
        if (TipoPago == TipoPago.Credito && Cliente is null)
            throw new DomainException("Para fiar se debe indicar el cliente.");

        if (TipoPago != TipoPago.Efectivo)
            MontoRecibido = null;
        else if (MontoRecibido is not null && MontoRecibido < Total)
            throw new DomainException($"El efectivo recibido ({MontoRecibido:N0}) es menor que el total ({Total:N0}).");

        if (TipoPago == TipoPago.Credito)
            Cliente!.CargarCredito(Total);
    }
}
