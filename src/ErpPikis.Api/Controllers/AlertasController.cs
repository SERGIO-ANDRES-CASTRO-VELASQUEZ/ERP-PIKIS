using ErpPikis.Domain.Entities;
using ErpPikis.Domain.Enums;
using ErpPikis.Service.Alertas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpPikis.Api.Controllers;

[ApiController]
[Route("api/alertas")]
[Authorize(Roles = Rol.Administrador)]
public class AlertasController : ControllerBase
{
    private readonly IAlertaService _alertas;
    private readonly INotificacionService _notificaciones;

    public AlertasController(IAlertaService alertas, INotificacionService notificaciones)
    {
        _alertas = alertas;
        _notificaciones = notificaciones;
    }

    [HttpGet]
    public async Task<ActionResult<List<AlertaDto>>> Listar(
        [FromQuery] bool soloNoLeidas, [FromQuery] bool soloActivas, CancellationToken ct) =>
        Ok(await _alertas.ListarAsync(soloNoLeidas, soloActivas, ct));

    /// <summary>Contador para la campana del panel.</summary>
    [HttpGet("resumen")]
    public async Task<ActionResult<ResumenAlertasDto>> Resumen(CancellationToken ct) =>
        Ok(await _alertas.ResumenAsync(ct));

    [HttpPut("{id:int}/leida")]
    public async Task<IActionResult> MarcarLeida(int id, CancellationToken ct)
    {
        await _alertas.MarcarLeidaAsync(id, ct);
        return NoContent();
    }

    [HttpPut("leer-todas")]
    public async Task<ActionResult<int>> MarcarTodas(CancellationToken ct) =>
        Ok(await _alertas.MarcarTodasLeidasAsync(ct));

    /// <summary>Ejecuta ya la revisión que normalmente corre cada N minutos.</summary>
    [HttpPost("revisar")]
    public async Task<ActionResult<int>> Revisar(CancellationToken ct) =>
        Ok(await _alertas.RevisarTodoAsync(ct));

    [HttpGet("whatsapp/estado")]
    public async Task<ActionResult<EstadoWhatsAppDto>> EstadoWhatsApp(CancellationToken ct) =>
        Ok(await _notificaciones.EstadoAsync(ct));

    [HttpGet("whatsapp/notificaciones")]
    public async Task<ActionResult<List<NotificacionDto>>> Notificaciones([FromQuery] EstadoNotificacion? estado, CancellationToken ct) =>
        Ok(await _notificaciones.ListarAsync(estado, ct));

    [HttpGet("destinatarios")]
    public async Task<ActionResult<List<DestinatarioDto>>> Destinatarios(CancellationToken ct) =>
        Ok(await _notificaciones.ListarDestinatariosAsync(ct));

    [HttpPost("destinatarios")]
    public async Task<ActionResult<DestinatarioDto>> CrearDestinatario(CrearDestinatarioRequest request, CancellationToken ct)
    {
        var destinatario = await _notificaciones.CrearDestinatarioAsync(request, ct);
        return CreatedAtAction(nameof(Destinatarios), null, destinatario);
    }

    [HttpPut("destinatarios/{id:int}")]
    public async Task<ActionResult<DestinatarioDto>> ActualizarDestinatario(int id, ActualizarDestinatarioRequest request, CancellationToken ct) =>
        Ok(await _notificaciones.ActualizarDestinatarioAsync(id, request, ct));

    [HttpDelete("destinatarios/{id:int}")]
    public async Task<IActionResult> EliminarDestinatario(int id, CancellationToken ct)
    {
        await _notificaciones.EliminarDestinatarioAsync(id, ct);
        return NoContent();
    }

    /// <summary>Envía un WhatsApp de prueba inmediato para verificar el número.</summary>
    [HttpPost("destinatarios/{id:int}/prueba")]
    public async Task<IActionResult> Probar(int id, CancellationToken ct)
    {
        await _notificaciones.EnviarPruebaAsync(id, ct);
        return NoContent();
    }
}
