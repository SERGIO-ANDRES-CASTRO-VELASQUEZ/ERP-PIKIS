using ErpPikis.Domain.Entities;
using ErpPikis.Domain.Enums;
using ErpPikis.Repository.Context;
using ErpPikis.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ErpPikis.Repository.Repositories;

public class CajaRepository : ICajaRepository
{
    private readonly ErpPikisDbContext _db;

    public CajaRepository(ErpPikisDbContext db) => _db = db;

    public Task<TurnoCaja?> ObtenerTurnoAbiertoAsync(int usuarioId, CancellationToken ct = default) =>
        _db.TurnosCaja.Include(t => t.Usuario)
            .FirstOrDefaultAsync(t => t.UsuarioId == usuarioId && t.FechaCierre == null, ct);

    public Task<TurnoCaja?> ObtenerTurnoPorIdAsync(int id, CancellationToken ct = default) =>
        _db.TurnosCaja.Include(t => t.Usuario).FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<List<TurnoCaja>> ListarTurnosAbiertosAsync(CancellationToken ct = default) =>
        _db.TurnosCaja.Include(t => t.Usuario).Where(t => t.FechaCierre == null).ToListAsync(ct);

    public Task<List<TurnoCaja>> ListarTurnosAsync(DateTime desdeUtc, DateTime hastaUtc, CancellationToken ct = default) =>
        _db.TurnosCaja.AsNoTracking()
            .Include(t => t.Usuario)
            .Where(t => t.FechaApertura >= desdeUtc && t.FechaApertura < hastaUtc)
            .OrderByDescending(t => t.FechaApertura)
            .ToListAsync(ct);

    public async Task<decimal> SumarMovimientosAsync(int turnoId, TipoMovimientoCaja tipo, CancellationToken ct = default) =>
        await _db.MovimientosCaja
            .Where(m => m.TurnoCajaId == turnoId && m.Tipo == tipo)
            .SumAsync(m => (decimal?)m.Monto, ct) ?? 0m;

    public async Task<decimal> SumarMovimientosAsync(DateTime desdeUtc, DateTime hastaUtc, TipoMovimientoCaja tipo, CancellationToken ct = default) =>
        await _db.MovimientosCaja
            .Where(m => m.Fecha >= desdeUtc && m.Fecha < hastaUtc && m.Tipo == tipo)
            .SumAsync(m => (decimal?)m.Monto, ct) ?? 0m;

    public Task<List<MovimientoCaja>> ListarMovimientosAsync(int turnoId, CancellationToken ct = default) =>
        _db.MovimientosCaja.AsNoTracking()
            .Include(m => m.Cliente)
            .Where(m => m.TurnoCajaId == turnoId)
            .OrderBy(m => m.Fecha)
            .ToListAsync(ct);

    public void AgregarTurno(TurnoCaja turno) => _db.TurnosCaja.Add(turno);

    public void AgregarMovimiento(MovimientoCaja movimiento) => _db.MovimientosCaja.Add(movimiento);
}
