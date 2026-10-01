using ErpPikis.Repository.Context;
using ErpPikis.Repository.Interfaces;

namespace ErpPikis.Repository.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ErpPikisDbContext _db;

    public UnitOfWork(ErpPikisDbContext db) => _db = db;

    public Task GuardarCambiosAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
