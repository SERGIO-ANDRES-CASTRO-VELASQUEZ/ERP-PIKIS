using ErpPikis.Domain.Entities;
using ErpPikis.Repository.Context;
using ErpPikis.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ErpPikis.Repository.Repositories;

public class CategoriaRepository : ICategoriaRepository
{
    private readonly ErpPikisDbContext _db;

    public CategoriaRepository(ErpPikisDbContext db) => _db = db;

    public Task<List<Categoria>> ListarAsync(CancellationToken ct = default) =>
        _db.Categorias.AsNoTracking().OrderBy(c => c.Nombre).ToListAsync(ct);

    public Task<Categoria?> ObtenerPorIdAsync(int id, CancellationToken ct = default) =>
        _db.Categorias.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<bool> ExisteNombreAsync(string nombre, CancellationToken ct = default) =>
        _db.Categorias.AnyAsync(c => c.Nombre.ToLower() == nombre.ToLower(), ct);

    public void Agregar(Categoria categoria) => _db.Categorias.Add(categoria);
}
