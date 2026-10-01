using ErpPikis.Domain.Entities;
using ErpPikis.Domain.Enums;
using ErpPikis.Domain.Exceptions;
using ErpPikis.Repository.Interfaces;
using ErpPikis.Service.Exceptions;
using Microsoft.Extensions.Logging;

namespace ErpPikis.Service.Alertas;

public interface INotificacionService
{
    Task<int> ProcesarPendientesAsync(CancellationToken ct = default);
    Task<List<NotificacionDto>> ListarAsync(EstadoNotificacion? estado, CancellationToken ct = default);
    Task<EstadoWhatsAppDto> EstadoAsync(CancellationToken ct = default);

    Task<List<DestinatarioDto>> ListarDestinatariosAsync(CancellationToken ct = default);
    Task<DestinatarioDto> CrearDestinatarioAsync(CrearDestinatarioRequest request, CancellationToken ct = default);
    Task<DestinatarioDto> ActualizarDestinatarioAsync(int id, ActualizarDestinatarioRequest request, CancellationToken ct = default);
    Task EliminarDestinatarioAsync(int id, CancellationToken ct = default);
    Task EnviarPruebaAsync(int id, CancellationToken ct = default);
}

public class NotificacionService : INotificacionService
{
    private readonly IAlertaRepository _alertas;
    private readonly IUnitOfWork _uow;
    private readonly IWhatsAppSender _whatsApp;
    private readonly AlertasOptions _opciones;
    private readonly EnvioWhatsAppOptions _envio;
    private readonly ILogger<NotificacionService> _logger;

    public NotificacionService(
        IAlertaRepository alertas,
        IUnitOfWork uow,
        IWhatsAppSender whatsApp,
        AlertasOptions opciones,
        EnvioWhatsAppOptions envio,
        ILogger<NotificacionService> logger)
    {
        _alertas = alertas;
        _uow = uow;
        _whatsApp = whatsApp;
        _opciones = opciones;
        _envio = envio;
        _logger = logger;
    }

    public async Task<int> ProcesarPendientesAsync(CancellationToken ct = default)
    {
        if (!_whatsApp.Habilitado)
            return 0;

        var pendientes = await _alertas.ListarPendientesAsync(DateTime.UtcNow, _envio.LoteMaximo, ct);
        var enviadas = 0;

        foreach (var n in pendientes)
        {
            try
            {
                await _whatsApp.EnviarAsync(n.Destinatario!.Telefono, n.Destinatario.ApiKey, n.Mensaje, ct);
                n.MarcarEnviada();
                enviadas++;
            }
            catch (WhatsAppException e)
            {
                n.RegistrarFallo(e.Message, _envio.MaxIntentos);
                _logger.LogWarning("WhatsApp a {Telefono} falló (intento {Intento}): {Error}",
                    n.Destinatario!.Telefono, n.Intentos, e.Message);
            }

            await _uow.GuardarCambiosAsync(ct);

            if (_envio.PausaEntreMensajesSegundos > 0)
                await Task.Delay(TimeSpan.FromSeconds(_envio.PausaEntreMensajesSegundos), ct);
        }

        return enviadas;
    }

    public async Task<List<NotificacionDto>> ListarAsync(EstadoNotificacion? estado, CancellationToken ct = default) =>
        (await _alertas.ListarNotificacionesAsync(estado, 200, ct))
        .Select(n => new NotificacionDto(
            n.Id, n.Destinatario?.Nombre ?? string.Empty, n.Destinatario?.Telefono ?? string.Empty,
            n.Mensaje, n.Estado.ToString(), n.Intentos, n.UltimoError, n.FechaCreacion, n.FechaEnvio))
        .ToList();

    public async Task<EstadoWhatsAppDto> EstadoAsync(CancellationToken ct = default) =>
        new(_whatsApp.Proveedor, _whatsApp.Habilitado, (await _alertas.ListarDestinatariosAsync(true, ct)).Count);

    public async Task<List<DestinatarioDto>> ListarDestinatariosAsync(CancellationToken ct = default) =>
        (await _alertas.ListarDestinatariosAsync(false, ct)).Select(ADto).ToList();

    public async Task<DestinatarioDto> CrearDestinatarioAsync(CrearDestinatarioRequest request, CancellationToken ct = default)
    {
        var telefono = NormalizarTelefono(request.Telefono);
        if (await _alertas.ExisteTelefonoAsync(telefono, null, ct))
            throw new DomainException($"El número {telefono} ya está registrado.");

        var destinatario = new DestinatarioAlerta
        {
            Nombre = request.Nombre.Trim(),
            Telefono = telefono,
            ApiKey = string.IsNullOrWhiteSpace(request.ApiKey) ? null : request.ApiKey.Trim()
        };
        _alertas.AgregarDestinatario(destinatario);
        await _uow.GuardarCambiosAsync(ct);
        return ADto(destinatario);
    }

    public async Task<DestinatarioDto> ActualizarDestinatarioAsync(int id, ActualizarDestinatarioRequest request, CancellationToken ct = default)
    {
        var destinatario = await ObtenerAsync(id, ct);
        var telefono = NormalizarTelefono(request.Telefono);
        if (await _alertas.ExisteTelefonoAsync(telefono, id, ct))
            throw new DomainException($"El número {telefono} ya está registrado.");

        destinatario.Nombre = request.Nombre.Trim();
        destinatario.Telefono = telefono;
        if (request.ApiKey is not null)
            destinatario.ApiKey = string.IsNullOrWhiteSpace(request.ApiKey) ? null : request.ApiKey.Trim();
        destinatario.Activo = request.Activo;

        await _uow.GuardarCambiosAsync(ct);
        return ADto(destinatario);
    }

    public async Task EliminarDestinatarioAsync(int id, CancellationToken ct = default)
    {
        _alertas.EliminarDestinatario(await ObtenerAsync(id, ct));
        await _uow.GuardarCambiosAsync(ct);
    }

    public async Task EnviarPruebaAsync(int id, CancellationToken ct = default)
    {
        var destinatario = await ObtenerAsync(id, ct);
        try
        {
            await _whatsApp.EnviarAsync(destinatario.Telefono, destinatario.ApiKey,
                $"*ERP-PIKIS* ✅ Mensaje de prueba\nHola {destinatario.Nombre}, desde ahora recibirá las alertas de la tienda por aquí.", ct);
        }
        catch (WhatsAppException e)
        {
            throw new DomainException($"No se pudo enviar el WhatsApp de prueba: {e.Message}");
        }
    }

    private async Task<DestinatarioAlerta> ObtenerAsync(int id, CancellationToken ct) =>
        await _alertas.ObtenerDestinatarioAsync(id, ct) ?? throw new NoEncontradoException("Destinatario no encontrado.");

    private string NormalizarTelefono(string telefono)
    {
        var digitos = new string(telefono.Where(char.IsDigit).ToArray());

        if (digitos.Length == 10 && digitos.StartsWith('3'))
            digitos = _opciones.PrefijoPais + digitos;

        if (digitos.Length is < 11 or > 15)
            throw new DomainException("Teléfono inválido. Use el celular con indicativo, por ejemplo 573001234567.");

        return digitos;
    }

    private static DestinatarioDto ADto(DestinatarioAlerta d) =>
        new(d.Id, d.Nombre, d.Telefono, !string.IsNullOrEmpty(d.ApiKey), d.Activo);
}
