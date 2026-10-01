using ErpPikis.Domain.Entities;
using ErpPikis.Domain.Enums;

namespace ErpPikis.Repository.Interfaces;

public interface IAlertaRepository
{
    Task<Alerta?> ObtenerPorIdAsync(int id, CancellationToken ct = default);
    Task<Alerta?> ObtenerActivaPorClaveAsync(string clave, CancellationToken ct = default);
    Task<bool> ExisteClaveAsync(string clave, CancellationToken ct = default);
    Task<List<Alerta>> ListarActivasAsync(TipoAlerta tipo, CancellationToken ct = default);
    Task<List<Alerta>> ListarAsync(bool soloNoLeidas, bool soloActivas, int maximo, CancellationToken ct = default);
    Task<Dictionary<SeveridadAlerta, int>> ContarNoLeidasAsync(CancellationToken ct = default);
    Task<int> MarcarTodasLeidasAsync(CancellationToken ct = default);
    void Agregar(Alerta alerta);

    Task<List<DestinatarioAlerta>> ListarDestinatariosAsync(bool soloActivos, CancellationToken ct = default);
    Task<DestinatarioAlerta?> ObtenerDestinatarioAsync(int id, CancellationToken ct = default);
    Task<bool> ExisteTelefonoAsync(string telefono, int? excluirId, CancellationToken ct = default);
    void AgregarDestinatario(DestinatarioAlerta destinatario);
    void EliminarDestinatario(DestinatarioAlerta destinatario);

    void AgregarNotificacion(NotificacionWhatsApp notificacion);
    Task<List<NotificacionWhatsApp>> ListarPendientesAsync(DateTime ahoraUtc, int maximo, CancellationToken ct = default);
    Task<List<NotificacionWhatsApp>> ListarNotificacionesAsync(EstadoNotificacion? estado, int maximo, CancellationToken ct = default);
}
