using ErpPikis.Domain.Enums;
using ErpPikis.Repository.Interfaces;
using ErpPikis.Service.Common;
using ErpPikis.Service.Dtos;
using ErpPikis.Service.Interfaces;

namespace ErpPikis.Service.Services;

public class DashboardService : IDashboardService
{
    private readonly IVentaRepository _ventas;
    private readonly IProductoRepository _productos;
    private readonly IClienteRepository _clientes;
    private readonly ICajaRepository _caja;

    public DashboardService(
        IVentaRepository ventas,
        IProductoRepository productos,
        IClienteRepository clientes,
        ICajaRepository caja)
    {
        _ventas = ventas;
        _productos = productos;
        _clientes = clientes;
        _caja = caja;
    }

    public async Task<DashboardDto> ObtenerAsync(DateOnly? fecha, CancellationToken ct = default)
    {
        var dia = fecha ?? RangoFechas.HoyLocal();
        var (desdeDia, hastaDia) = RangoFechas.DiasLocalesEnUtc(dia, dia);
        var (desde7, hasta7) = RangoFechas.DiasLocalesEnUtc(dia.AddDays(-6), dia);

        var porTipo = await _ventas.TotalesPorTipoPagoAsync(desdeDia, hastaDia, ct);
        var ventasCredito = porTipo.GetValueOrDefault(TipoPago.Credito);
        var ventasDelDia = porTipo.Values.Sum();

        var ventas7 = await _ventas.TotalesPorVentaAsync(desde7, hasta7, ct);
        var porDia = ventas7
            .GroupBy(v => RangoFechas.DiaLocal(v.FechaUtc))
            .ToDictionary(g => g.Key, g => (Total: g.Sum(v => v.Total), Numero: g.Count()));

        var ultimos7 = Enumerable.Range(0, 7)
            .Select(i => dia.AddDays(i - 6))
            .Select(d => porDia.TryGetValue(d, out var x)
                ? new VentasPorDiaDto(d, x.Total, x.Numero)
                : new VentasPorDiaDto(d, 0, 0))
            .ToList();

        var bajoStock = await _productos.ListarAsync(null, soloBajoStock: true, ct);
        var masVendidos = await _ventas.MasVendidosAsync(desde7, hasta7, 5, ct);

        return new DashboardDto(
            dia,
            ventasDelDia,
            porDia.TryGetValue(dia, out var hoy) ? hoy.Numero : 0,
            ventasDelDia - ventasCredito,
            ventasCredito,
            await _caja.SumarMovimientosAsync(desdeDia, hastaDia, TipoMovimientoCaja.Ingreso, ct),
            await _caja.SumarMovimientosAsync(desdeDia, hastaDia, TipoMovimientoCaja.Egreso, ct),
            await _ventas.UtilidadBrutaAsync(desdeDia, hastaDia, ct),
            await _clientes.TotalCarteraAsync(ct),
            await _productos.ValorInventarioAsync(ct),
            bajoStock.Select(p => new AlertaStockDto(p.Id, p.Nombre, p.StockActual, p.StockMinimo)).ToList(),
            ultimos7,
            masVendidos.Select(m => new ProductoMasVendidoDto(m.ProductoId, m.Nombre, m.Cantidad, m.Total)).ToList());
    }
}
