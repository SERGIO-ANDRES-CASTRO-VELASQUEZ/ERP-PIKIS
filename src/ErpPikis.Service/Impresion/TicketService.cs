using ErpPikis.Domain.Entities;
using ErpPikis.Domain.Enums;
using ErpPikis.Domain.Exceptions;
using ErpPikis.Repository.Interfaces;
using ErpPikis.Service.Exceptions;

namespace ErpPikis.Service.Impresion;

public interface ITicketService
{
    Task<byte[]> GenerarPdfAsync(int ventaId, int? soloDelUsuarioId, int? anchoPapelMm, CancellationToken ct = default);
    Task<byte[]> GenerarEscPosAsync(int ventaId, int? soloDelUsuarioId, CancellationToken ct = default);
    Task ImprimirAsync(int ventaId, int? soloDelUsuarioId, bool esVentaNueva, CancellationToken ct = default);
}

public class TicketService : ITicketService
{
    private readonly IVentaRepository _ventas;
    private readonly IImpresoraTermica _impresora;
    private readonly TiendaOptions _tienda;
    private readonly ImpresoraOptions _opciones;

    public TicketService(
        IVentaRepository ventas,
        IImpresoraTermica impresora,
        TiendaOptions tienda,
        ImpresoraOptions opciones)
    {
        _ventas = ventas;
        _impresora = impresora;
        _tienda = tienda;
        _opciones = opciones;
    }

    public async Task<byte[]> GenerarPdfAsync(int ventaId, int? soloDelUsuarioId, int? anchoPapelMm, CancellationToken ct = default)
    {
        var ancho = anchoPapelMm ?? _opciones.AnchoPapelMm;
        if (ancho is not (58 or 80))
            throw new DomainException("El ancho de papel debe ser 58 u 80 mm.");

        var venta = await ObtenerVentaAsync(ventaId, soloDelUsuarioId, ct);
        return TicketPdfRenderer.Generar(Construir(venta, esReimpresion: false), ancho);
    }

    public async Task<byte[]> GenerarEscPosAsync(int ventaId, int? soloDelUsuarioId, CancellationToken ct = default)
    {
        var venta = await ObtenerVentaAsync(ventaId, soloDelUsuarioId, ct);
        return TicketEscPosRenderer.Generar(Construir(venta, esReimpresion: false), _opciones, abrirCajon: false);
    }

    public async Task ImprimirAsync(int ventaId, int? soloDelUsuarioId, bool esVentaNueva, CancellationToken ct = default)
    {
        var venta = await ObtenerVentaAsync(ventaId, soloDelUsuarioId, ct);
        var abrirCajon = esVentaNueva && _opciones.AbrirCajonEnVentaEfectivo && venta.TipoPago == TipoPago.Efectivo;
        var datos = TicketEscPosRenderer.Generar(Construir(venta, esReimpresion: !esVentaNueva), _opciones, abrirCajon);
        await _impresora.EnviarAsync(datos, ct);
    }

    private async Task<Venta> ObtenerVentaAsync(int ventaId, int? soloDelUsuarioId, CancellationToken ct)
    {
        var venta = await _ventas.ObtenerPorIdAsync(ventaId, ct);
        if (venta is null || (soloDelUsuarioId is not null && venta.UsuarioId != soloDelUsuarioId))
            throw new NoEncontradoException("Venta no encontrada.");
        return venta;
    }

    private TicketVenta Construir(Venta v, bool esReimpresion) => new(
        _tienda,
        v.Id,
        DateTime.SpecifyKind(v.Fecha, DateTimeKind.Utc).ToLocalTime(),
        v.Usuario?.Nombre ?? string.Empty,
        v.TipoPago.ToString(),
        v.TipoPago == TipoPago.Credito,
        v.Cliente?.Nombre,
        v.Cliente?.SaldoActual,
        v.Detalles
            .OrderBy(d => d.Id)
            .Select(d => new LineaTicket(d.Producto?.Nombre ?? $"Producto {d.ProductoId}", d.Cantidad, d.PrecioUnitario, d.Subtotal))
            .ToList(),
        v.Total,
        v.MontoRecibido,
        v.Cambio,
        esReimpresion);
}
