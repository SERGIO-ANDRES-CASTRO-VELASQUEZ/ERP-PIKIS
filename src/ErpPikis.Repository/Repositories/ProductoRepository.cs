using ErpPikis.Domain.Entities;
using ErpPikis.Repository.Context;
using ErpPikis.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ErpPikis.Repository.Repositories;

public class ProductoRepository : IProductoRepository
{
    private readonly ErpPikisDbContext _db;

    public ProductoRepository(ErpPikisDbContext db) => _db = db;

    public Task<List<Producto>> ListarAsync(string? busqueda, bool soloBajoStock, CancellationToken ct = default)
    {
        var query = _db.Productos.AsNoTracking().Include(p => p.Categoria).Where(p => p.Activo);

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var patron = $"%{busqueda.Trim()}%";
            query = query.Where(p => EF.Functions.ILike(p.Nombre, patron) || p.CodigoBarras == busqueda.Trim());
        }

        if (soloBajoStock)
            query = query.Where(p => p.StockActual <= p.StockMinimo);

        return query.OrderBy(p => p.Nombre).ToListAsync(ct);
    }

    public Task<Producto?> ObtenerPorIdAsync(int id, CancellationToken ct = default) =>
        _db.Productos.Include(p => p.Categoria).FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Producto?> ObtenerPorCodigoBarrasAsync(string codigoBarras, CancellationToken ct = default) =>
        _db.Productos.AsNoTracking().Include(p => p.Categoria)
            .FirstOrDefaultAsync(p => p.CodigoBarras == codigoBarras && p.Activo, ct);

    public Task<List<Producto>> ObtenerPorIdsAsync(IEnumerable<int> ids, CancellationToken ct = default)
    {
        var lista = ids.Distinct().ToList();
        return _db.Productos.Where(p => lista.Contains(p.Id)).ToListAsync(ct);
    }

    public Task<List<int>> ListarIdsAsync(CancellationToken ct = default) =>
        _db.Productos.Select(p => p.Id).ToListAsync(ct);

    public Task<bool> ExisteCodigoBarrasAsync(string codigoBarras, int? excluirId, CancellationToken ct = default) =>
        _db.Productos.AnyAsync(p => p.CodigoBarras == codigoBarras && (excluirId == null || p.Id != excluirId), ct);

    public async Task<decimal> ValorInventarioAsync(CancellationToken ct = default) =>
        await _db.Productos.Where(p => p.Activo).SumAsync(p => (decimal?)(p.StockActual * p.PrecioCompra), ct) ?? 0m;

    public Task<List<MovimientoInventario>> ListarMovimientosAsync(int productoId, CancellationToken ct = default) =>
        _db.MovimientosInventario.AsNoTracking()
            .Include(m => m.Usuario)
            .Where(m => m.ProductoId == productoId)
            .OrderByDescending(m => m.Fecha)
            .Take(200)
            .ToListAsync(ct);

    public void Agregar(Producto producto) => _db.Productos.Add(producto);

    public void AgregarMovimiento(MovimientoInventario movimiento) => _db.MovimientosInventario.Add(movimiento);
}
