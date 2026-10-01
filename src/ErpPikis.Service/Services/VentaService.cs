using ErpPikis.Domain.Entities;
using ErpPikis.Domain.Enums;
using ErpPikis.Domain.Exceptions;
using ErpPikis.Repository.Interfaces;
using ErpPikis.Service.Alertas;
using ErpPikis.Service.Common;
using ErpPikis.Service.Dtos;
using ErpPikis.Service.Exceptions;
using ErpPikis.Service.Interfaces;

namespace ErpPikis.Service.Services;

public class VentaService : IVentaService
{
    private readonly IVentaRepository _ventas;
    private readonly IProductoRepository _productos;
    private readonly IClienteRepository _clientes;
    private readonly ICajaRepository _caja;
    private readonly IUnitOfWork _uow;
    private readonly IAlertaService _alertas;

    public VentaService(
        IVentaRepository ventas,
        IProductoRepository productos,
        IClienteRepository clientes,
        ICajaRepository caja,
        IUnitOfWork uow,
        IAlertaService alertas)
    {
        _ventas = ventas;
        _productos = productos;
        _clientes = clientes;
        _caja = caja;
        _uow = uow;
        _alertas = alertas;
    }

    public async Task<VentaDto> RegistrarAsync(RegistrarVentaRequest request, int usuarioId, CancellationToken ct = default)
    {
        if (!Enum.IsDefined(request.TipoPago))
            throw new DomainException("Tipo de pago inválido.");

        var turno = await _caja.ObtenerTurnoAbiertoAsync(usuarioId, ct)
            ?? throw new DomainException("Debe abrir caja antes de registrar ventas.");

        var venta = new Venta
        {
            UsuarioId = usuarioId,
            Usuario = turno.Usuario,
            TurnoCajaId = turno.Id,
            TipoPago = request.TipoPago,
            MontoRecibido = request.MontoRecibido
        };

        if (request.TipoPago == TipoPago.Credito)
        {
            if (request.ClienteId is null)
                throw new DomainException("Para fiar se debe indicar el cliente.");

            venta.Cliente = await _clientes.ObtenerPorIdAsync(request.ClienteId.Value, ct)
                ?? throw new NoEncontradoException("Cliente no encontrado.");
            venta.ClienteId = venta.Cliente.Id;
        }

        var items = request.Items
            .GroupBy(i => i.ProductoId)
            .Select(g => (ProductoId: g.Key, Cantidad: g.Sum(i => i.Cantidad)))
            .ToList();

        var productos = (await _productos.ObtenerPorIdsAsync(items.Select(i => i.ProductoId), ct))
            .ToDictionary(p => p.Id);

        foreach (var (productoId, cantidad) in items)
        {
            if (!productos.TryGetValue(productoId, out var producto) || !producto.Activo)
                throw new NoEncontradoException($"Producto {productoId} no encontrado o inactivo.");

            venta.AgregarProducto(producto, cantidad);
            _productos.AgregarMovimiento(new MovimientoInventario
            {
                ProductoId = producto.Id,
                Tipo = TipoMovimientoInventario.Venta,
                Cantidad = -cantidad,
                CostoUnitario = producto.PrecioCompra,
                UsuarioId = usuarioId
            });
        }

        venta.ValidarPago();

        _ventas.Agregar(venta);
        await _uow.GuardarCambiosAsync(ct);

        await _alertas.EvaluarProductosAsync(productos.Keys, ct);
        if (venta.ClienteId is { } clienteId)
            await _alertas.EvaluarClientesAsync([clienteId], ct);

        return venta.ADto();
    }

    public async Task<VentaDto> ObtenerAsync(int id, int? soloDelUsuarioId, CancellationToken ct = default)
    {
        var venta = await _ventas.ObtenerPorIdAsync(id, ct);

        if (venta is null || (soloDelUsuarioId is not null && venta.UsuarioId != soloDelUsuarioId))
            throw new NoEncontradoException("Venta no encontrada.");

        return venta.ADto();
    }

    public async Task<List<VentaResumenDto>> ListarAsync(DateOnly desde, DateOnly hasta, int? usuarioId, CancellationToken ct = default)
    {
        if (hasta < desde)
            throw new DomainException("La fecha final no puede ser anterior a la inicial.");

        var (desdeUtc, hastaUtc) = RangoFechas.DiasLocalesEnUtc(desde, hasta);
        return (await _ventas.ListarAsync(desdeUtc, hastaUtc, usuarioId, ct)).Select(v => v.AResumen()).ToList();
    }
}
