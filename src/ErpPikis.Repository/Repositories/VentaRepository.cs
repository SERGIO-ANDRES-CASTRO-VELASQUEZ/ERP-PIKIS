using ErpPikis.Domain.Entities;
using ErpPikis.Domain.Enums;
using ErpPikis.Repository.Context;
using ErpPikis.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ErpPikis.Repository.Repositories;

public class VentaRepository : IVentaRepository
{
    private readonly ErpPikisDbContext _db;

    public VentaRepository(ErpPikisDbContext db) => _db = db;

    public Task<Venta?> ObtenerPorIdAsync(int id, CancellationToken ct = default) =>
        _db.Ventas.AsNoTracking()
            .Include(v => v.Usuario)
            .Include(v => v.Cliente)
            .Include(v => v.Detalles).ThenInclude(d => d.Producto)
            .FirstOrDefaultAsync(v => v.Id == id, ct);

    public Task<List<Venta>> ListarAsync(DateTime desdeUtc, DateTime hastaUtc, int? usuarioId, CancellationToken ct = default)
    {
        var query = _db.Ventas.AsNoTracking()
            .Include(v => v.Usuario)
            .Include(v => v.Cliente)
            .Where(v => v.Fecha >= desdeUtc && v.Fecha < hastaUtc);

        if (usuarioId is not null)
            query = query.Where(v => v.UsuarioId == usuarioId);

        return query.OrderByDescending(v => v.Fecha).ToListAsync(ct);
    }

    public async Task<decimal> TotalPorTipoPagoEnTurnoAsync(int turnoId, TipoPago tipoPago, CancellationToken ct = default) =>
        await _db.Ventas
            .Where(v => v.TurnoCajaId == turnoId && v.TipoPago == tipoPago)
            .SumAsync(v => (decimal?)v.Total, ct) ?? 0m;

    public async Task<Dictionary<TipoPago, decimal>> TotalesPorTipoPagoAsync(DateTime desdeUtc, DateTime hastaUtc, CancellationToken ct = default)
    {
        var filas = await _db.Ventas
            .Where(v => v.Fecha >= desdeUtc && v.Fecha < hastaUtc)
            .GroupBy(v => v.TipoPago)
            .Select(g => new { TipoPago = g.Key, Total = g.Sum(v => v.Total) })
            .ToListAsync(ct);

        return filas.ToDictionary(f => f.TipoPago, f => f.Total);
    }

    public async Task<decimal> UtilidadBrutaAsync(DateTime desdeUtc, DateTime hastaUtc, CancellationToken ct = default) =>
        await _db.DetallesVenta
            .Where(d => d.Venta!.Fecha >= desdeUtc && d.Venta.Fecha < hastaUtc)
            .SumAsync(d => (decimal?)(d.Subtotal - d.CostoUnitario * d.Cantidad), ct) ?? 0m;

    public async Task<List<(DateTime FechaUtc, decimal Total)>> TotalesPorVentaAsync(DateTime desdeUtc, DateTime hastaUtc, CancellationToken ct = default)
    {
        var filas = await _db.Ventas
            .Where(v => v.Fecha >= desdeUtc && v.Fecha < hastaUtc)
            .Select(v => new { v.Fecha, v.Total })
            .ToListAsync(ct);

        return filas.Select(f => (f.Fecha, f.Total)).ToList();
    }

    public async Task<List<(int ProductoId, string Nombre, int Cantidad, decimal Total)>> MasVendidosAsync(
        DateTime desdeUtc, DateTime hastaUtc, int top, CancellationToken ct = default)
    {
        var filas = await _db.DetallesVenta
            .Where(d => d.Venta!.Fecha >= desdeUtc && d.Venta.Fecha < hastaUtc)
            .GroupBy(d => new { d.ProductoId, d.Producto!.Nombre })
            .Select(g => new
            {
                g.Key.ProductoId,
                g.Key.Nombre,
                Cantidad = g.Sum(d => d.Cantidad),
                Total = g.Sum(d => d.Subtotal)
            })
            .OrderByDescending(x => x.Cantidad)
            .Take(top)
            .ToListAsync(ct);

        return filas.Select(f => (f.ProductoId, f.Nombre, f.Cantidad, f.Total)).ToList();
    }

    public void Agregar(Venta venta) => _db.Ventas.Add(venta);
}
