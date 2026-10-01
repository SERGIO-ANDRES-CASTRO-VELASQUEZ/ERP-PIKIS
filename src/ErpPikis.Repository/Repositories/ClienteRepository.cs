using ErpPikis.Domain.Entities;
using ErpPikis.Domain.Enums;
using ErpPikis.Repository.Context;
using ErpPikis.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ErpPikis.Repository.Repositories;

public class ClienteRepository : IClienteRepository
{
    private readonly ErpPikisDbContext _db;

    public ClienteRepository(ErpPikisDbContext db) => _db = db;

    public Task<List<Cliente>> ListarAsync(string? busqueda, CancellationToken ct = default)
    {
        var query = _db.Clientes.AsNoTracking().Where(c => c.Activo);

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var patron = $"%{busqueda.Trim()}%";
            query = query.Where(c => EF.Functions.ILike(c.Nombre, patron) || c.Telefono == busqueda.Trim());
        }

        return query.OrderBy(c => c.Nombre).ToListAsync(ct);
    }

    public Task<Cliente?> ObtenerPorIdAsync(int id, CancellationToken ct = default) =>
        _db.Clientes.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<decimal> TotalCarteraAsync(CancellationToken ct = default) =>
        await _db.Clientes.SumAsync(c => (decimal?)c.SaldoActual, ct) ?? 0m;

    public Task<List<Cliente>> ObtenerPorIdsAsync(IEnumerable<int> ids, CancellationToken ct = default)
    {
        var lista = ids.Distinct().ToList();
        return _db.Clientes.Where(c => lista.Contains(c.Id)).ToListAsync(ct);
    }

    public async Task<List<(Cliente Cliente, DateTime? ReferenciaUtc)>> ListarDeudoresAsync(
        IEnumerable<int>? ids, CancellationToken ct = default)
    {
        var query = _db.Clientes.Where(c => c.SaldoActual > 0);
        if (ids is not null)
        {
            var lista = ids.Distinct().ToList();
            query = query.Where(c => lista.Contains(c.Id));
        }

        var filas = await query
            .Select(c => new
            {
                Cliente = c,
                UltimoAbono = _db.MovimientosCaja
                    .Where(m => m.ClienteId == c.Id && m.Tipo == TipoMovimientoCaja.Ingreso)
                    .Max(m => (DateTime?)m.Fecha),
                PrimerFiado = _db.Ventas
                    .Where(v => v.ClienteId == c.Id && v.TipoPago == TipoPago.Credito)
                    .Min(v => (DateTime?)v.Fecha)
            })
            .ToListAsync(ct);

        return filas.Select(f => (f.Cliente, f.UltimoAbono ?? f.PrimerFiado)).ToList();
    }

    public void Agregar(Cliente cliente) => _db.Clientes.Add(cliente);
}
