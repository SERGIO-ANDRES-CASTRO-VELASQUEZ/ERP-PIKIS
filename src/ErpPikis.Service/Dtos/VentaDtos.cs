using System.ComponentModel.DataAnnotations;
using ErpPikis.Domain.Enums;

namespace ErpPikis.Service.Dtos;

public record ItemVentaRequest(
    [Range(1, int.MaxValue)] int ProductoId,
    [Range(1, 10_000)] int Cantidad);

public record RegistrarVentaRequest(
    [Required, MinLength(1)] List<ItemVentaRequest> Items,
    TipoPago TipoPago,
    int? ClienteId,
    [Range(0, 99_999_999)] decimal? MontoRecibido = null,
    bool Imprimir = false);

public record VentaRegistradaDto(VentaDto Venta, bool TicketImpreso, string? ErrorImpresion);

public record DetalleVentaDto(
    int ProductoId,
    string Producto,
    int Cantidad,
    decimal PrecioUnitario,
    decimal Subtotal);

public record VentaDto(
    int Id,
    DateTime Fecha,
    string TipoPago,
    decimal Total,
    decimal? MontoRecibido,
    decimal? Cambio,
    string Cajero,
    int? ClienteId,
    string? Cliente,
    List<DetalleVentaDto> Detalles);

public record VentaResumenDto(
    int Id,
    DateTime Fecha,
    string TipoPago,
    decimal Total,
    string Cajero,
    string? Cliente);
