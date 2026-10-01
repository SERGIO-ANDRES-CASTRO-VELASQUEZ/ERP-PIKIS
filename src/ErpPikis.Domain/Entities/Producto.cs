using ErpPikis.Domain.Exceptions;

namespace ErpPikis.Domain.Entities;

public class Producto
{
    public int Id { get; set; }
    public string? CodigoBarras { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal PrecioCompra { get; set; }
    public decimal PrecioVenta { get; set; }
    public int StockActual { get; set; }
    public int StockMinimo { get; set; }
    public bool Activo { get; set; } = true;

    public int CategoriaId { get; set; }
    public Categoria? Categoria { get; set; }

    public bool RequiereReabastecimiento => StockActual <= StockMinimo;

    public void FijarPrecios(decimal precioCompra, decimal precioVenta)
    {
        if (precioCompra < 0 || precioVenta < 0)
            throw new DomainException("Los precios no pueden ser negativos.");
        if (precioVenta < precioCompra)
            throw new DomainException(
                $"El precio de venta de {Nombre} no puede ser menor que el precio de compra (venta a pérdida).");

        PrecioCompra = precioCompra;
        PrecioVenta = precioVenta;
    }

    public void DescontarStock(int cantidad)
    {
        if (cantidad <= 0)
            throw new DomainException("La cantidad debe ser mayor que cero.");
        if (cantidad > StockActual)
            throw new DomainException(
                $"Stock insuficiente de {Nombre}. Disponible: {StockActual}, solicitado: {cantidad}.");

        StockActual -= cantidad;
    }

    public void AgregarStock(int cantidad)
    {
        if (cantidad <= 0)
            throw new DomainException("La cantidad debe ser mayor que cero.");

        StockActual += cantidad;
    }

    public void AjustarStock(int diferencia)
    {
        if (StockActual + diferencia < 0)
            throw new DomainException($"El ajuste dejaría el stock de {Nombre} en negativo.");

        StockActual += diferencia;
    }
}
