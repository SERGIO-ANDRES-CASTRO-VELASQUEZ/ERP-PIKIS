using ErpPikis.Domain.Entities;
using ErpPikis.Domain.Enums;
using ErpPikis.Repository.Context;
using ErpPikis.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ErpPikis.Repository.Repositories;

public class AlertaRepository : IAlertaRepository
{
    private readonly ErpPikisDbContext _db;

    public AlertaRepository(ErpPikisDbContext db) => _db = db;

    public Task<Alerta?> ObtenerPorIdAsync(int id, CancellationToken ct = default) =>
        _db.Alertas.FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<Alerta?> ObtenerActivaPorClaveAsync(string clave, CancellationToken ct = default) =>
        _db.Alertas.Local.FirstOrDefault(a => a.Clave == clave && a.Activa)
        ?? await _db.Alertas.FirstOrDefaultAsync(a => a.Clave == clave && a.Activa, ct);

    public async Task<bool> ExisteClaveAsync(string clave, CancellationToken ct = default) =>
        _db.Alertas.Local.Any(a => a.Clave == clave) || await _db.Alertas.AnyAsync(a => a.Clave == clave, ct);

    public Task<List<Alerta>> ListarActivasAsync(TipoAlerta tipo, CancellationToken ct = default) =>
        _db.Alertas.Where(a => a.Activa && a.Tipo == tipo).ToListAsync(ct);

    public Task<List<Alerta>> ListarAsync(bool soloNoLeidas, bool soloActivas, int maximo, CancellationToken ct = default)
    {
        var query = _db.Alertas.AsNoTracking();
        if (soloNoLeidas)
            query = query.Where(a => !a.Leida);
        if (soloActivas)
            query = query.Where(a => a.Activa);
        return query.OrderByDescending(a => a.Fecha).Take(maximo).ToListAsync(ct);
    }

    public async Task<Dictionary<SeveridadAlerta, int>> ContarNoLeidasAsync(CancellationToken ct = default)
    {
        var filas = await _db.Alertas
            .Where(a => !a.Leida)
            .GroupBy(a => a.Severidad)
            .Select(g => new { Severidad = g.Key, Total = g.Count() })
            .ToListAsync(ct);
        return filas.ToDictionary(f => f.Severidad, f => f.Total);
    }

    public Task<int> MarcarTodasLeidasAsync(CancellationToken ct = default) =>
        _db.Alertas
            .Where(a => !a.Leida)
            .ExecuteUpdateAsync(s => s
                .SetProperty(a => a.Leida, true)
                .SetProperty(a => a.FechaLeida, DateTime.UtcNow), ct);

    public void Agregar(Alerta alerta) => _db.Alertas.Add(alerta);

    public Task<List<DestinatarioAlerta>> ListarDestinatariosAsync(bool soloActivos, CancellationToken ct = default) =>
        _db.DestinatariosAlerta
            .Where(d => !soloActivos || d.Activo)
            .OrderBy(d => d.Nombre)
            .ToListAsync(ct);

    public Task<DestinatarioAlerta?> ObtenerDestinatarioAsync(int id, CancellationToken ct = default) =>
        _db.DestinatariosAlerta.FirstOrDefaultAsync(d => d.Id == id, ct);

    public Task<bool> ExisteTelefonoAsync(string telefono, int? excluirId, CancellationToken ct = default) =>
        _db.DestinatariosAlerta.AnyAsync(d => d.Telefono == telefono && (excluirId == null || d.Id != excluirId), ct);

    public void AgregarDestinatario(DestinatarioAlerta destinatario) => _db.DestinatariosAlerta.Add(destinatario);

    public void EliminarDestinatario(DestinatarioAlerta destinatario) => _db.DestinatariosAlerta.Remove(destinatario);

    public void AgregarNotificacion(NotificacionWhatsApp notificacion) => _db.NotificacionesWhatsApp.Add(notificacion);

    public Task<List<NotificacionWhatsApp>> ListarPendientesAsync(DateTime ahoraUtc, int maximo, CancellationToken ct = default) =>
        _db.NotificacionesWhatsApp
            .Include(n => n.Destinatario)
            .Where(n => n.Estado == EstadoNotificacion.Pendiente && n.ProximoIntento <= ahoraUtc)
            .OrderBy(n => n.FechaCreacion)
            .Take(maximo)
            .ToListAsync(ct);

    public Task<List<NotificacionWhatsApp>> ListarNotificacionesAsync(EstadoNotificacion? estado, int maximo, CancellationToken ct = default) =>
        _db.NotificacionesWhatsApp.AsNoTracking()
            .Include(n => n.Destinatario)
            .Where(n => estado == null || n.Estado == estado)
            .OrderByDescending(n => n.FechaCreacion)
            .Take(maximo)
            .ToListAsync(ct);
}
