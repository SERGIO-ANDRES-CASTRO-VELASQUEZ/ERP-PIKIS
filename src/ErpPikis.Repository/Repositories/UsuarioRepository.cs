using ErpPikis.Domain.Entities;
using ErpPikis.Repository.Context;
using ErpPikis.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ErpPikis.Repository.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly ErpPikisDbContext _db;

    public UsuarioRepository(ErpPikisDbContext db) => _db = db;

    public Task<Usuario?> ObtenerPorIdAsync(int id, CancellationToken ct = default) =>
        _db.Usuarios.Include(u => u.Rol).FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<Usuario?> ObtenerPorUsernameAsync(string username, CancellationToken ct = default) =>
        _db.Usuarios.Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.Username == username.ToLower(), ct);

    public Task<List<Usuario>> ListarAsync(CancellationToken ct = default) =>
        _db.Usuarios.AsNoTracking().Include(u => u.Rol).OrderBy(u => u.Nombre).ToListAsync(ct);

    public Task<bool> ExisteUsernameAsync(string username, CancellationToken ct = default) =>
        _db.Usuarios.AnyAsync(u => u.Username == username.ToLower(), ct);

    public Task<bool> HayUsuariosAsync(CancellationToken ct = default) => _db.Usuarios.AnyAsync(ct);

    public Task<Rol?> ObtenerRolPorNombreAsync(string nombre, CancellationToken ct = default) =>
        _db.Roles.FirstOrDefaultAsync(r => r.Nombre == nombre, ct);

    public void Agregar(Usuario usuario) => _db.Usuarios.Add(usuario);
}
