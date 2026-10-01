using System.ComponentModel.DataAnnotations;

namespace ErpPikis.Service.Alertas;

public record AlertaDto(
    int Id,
    string Tipo,
    string Severidad,
    string Titulo,
    string Mensaje,
    DateTime Fecha,
    bool Leida,
    bool Activa);

public record ResumenAlertasDto(int NoLeidas, int Criticas, int Advertencias, int Informativas);

public record DestinatarioDto(int Id, string Nombre, string Telefono, bool TieneApiKey, bool Activo);

public record CrearDestinatarioRequest(
    [Required, StringLength(120)] string Nombre,
    [Required, StringLength(20)] string Telefono,
    [StringLength(100)] string? ApiKey);

public record ActualizarDestinatarioRequest(
    [Required, StringLength(120)] string Nombre,
    [Required, StringLength(20)] string Telefono,
    [StringLength(100)] string? ApiKey,
    bool Activo);

public record NotificacionDto(
    int Id,
    string Destinatario,
    string Telefono,
    string Mensaje,
    string Estado,
    int Intentos,
    string? UltimoError,
    DateTime FechaCreacion,
    DateTime? FechaEnvio);

public record EstadoWhatsAppDto(string Proveedor, bool Habilitado, int DestinatariosActivos);
