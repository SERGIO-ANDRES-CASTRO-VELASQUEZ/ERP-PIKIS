using ErpPikis.Domain.Entities;
using ErpPikis.Service.Dtos;

namespace ErpPikis.Service.Common;

internal static class Mapeos
{
    public static UsuarioDto ADto(this Usuario u) =>
        new(u.Id, u.Nombre, u.Username, u.Rol?.Nombre ?? string.Empty, u.Activo);

    public static CategoriaDto ADto(this Categoria c) => new(c.Id, c.Nombre);

    public static ProductoDto ADto(this Producto p) => new(
        p.Id,
        p.CodigoBarras,
        p.Nombre,
        p.CategoriaId,
        p.Categoria?.Nombre ?? string.Empty,
        p.PrecioCompra,
        p.PrecioVenta,
        p.PrecioVenta == 0 ? 0 : Math.Round((p.PrecioVenta - p.PrecioCompra) / p.PrecioVenta * 100, 2),
        p.StockActual,
        p.StockMinimo,
        p.RequiereReabastecimiento);

    public static ClienteDto ADto(this Cliente c) =>
        new(c.Id, c.Nombre, c.Telefono, c.LimiteCredito, c.SaldoActual, c.CreditoDisponible);

    public static VentaDto ADto(this Venta v) => new(
        v.Id,
        v.Fecha,
        v.TipoPago.ToString(),
        v.Total,
        v.MontoRecibido,
        v.Cambio,
        v.Usuario?.Nombre ?? string.Empty,
        v.ClienteId,
        v.Cliente?.Nombre,
        v.Detalles
            .Select(d => new DetalleVentaDto(d.ProductoId, d.Producto?.Nombre ?? string.Empty, d.Cantidad, d.PrecioUnitario, d.Subtotal))
            .ToList());

    public static VentaResumenDto AResumen(this Venta v) =>
        new(v.Id, v.Fecha, v.TipoPago.ToString(), v.Total, v.Usuario?.Nombre ?? string.Empty, v.Cliente?.Nombre);

    public static MovimientoCajaDto ADto(this MovimientoCaja m) =>
        new(m.Id, m.Fecha, m.Tipo.ToString(), m.Monto, m.Descripcion, m.Cliente?.Nombre);

    public static MovimientoInventarioDto ADto(this MovimientoInventario m) =>
        new(m.Id, m.Fecha, m.Tipo.ToString(), m.Cantidad, m.CostoUnitario, m.Motivo, m.Usuario?.Nombre ?? string.Empty);
}
