using ErpPikis.Api.Seguridad;
using ErpPikis.Service.Common;
using ErpPikis.Service.Dtos;
using ErpPikis.Service.Exceptions;
using ErpPikis.Service.Impresion;
using ErpPikis.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpPikis.Api.Controllers;

[ApiController]
[Route("api/ventas")]
[Authorize]
public class VentasController : ControllerBase
{
    private readonly IVentaService _ventas;
    private readonly ITicketService _tickets;
    private readonly ILogger<VentasController> _logger;

    public VentasController(IVentaService ventas, ITicketService tickets, ILogger<VentasController> logger)
    {
        _ventas = ventas;
        _tickets = tickets;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<VentaRegistradaDto>> Registrar(RegistrarVentaRequest request, CancellationToken ct)
    {
        var venta = await _ventas.RegistrarAsync(request, User.UsuarioId(), ct);

        var impreso = false;
        string? error = null;
        if (request.Imprimir)
        {
            try
            {
                await _tickets.ImprimirAsync(venta.Id, null, esVentaNueva: true, ct);
                impreso = true;
            }
            catch (ImpresoraException e)
            {
                _logger.LogWarning(e, "La venta {VentaId} se guardó pero no se pudo imprimir", venta.Id);
                error = e.Message;
            }
        }

        return CreatedAtAction(nameof(Obtener), new { id = venta.Id }, new VentaRegistradaDto(venta, impreso, error));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<VentaDto>> Obtener(int id, CancellationToken ct) =>
        Ok(await _ventas.ObtenerAsync(id, SoloDelUsuario(), ct));

    [HttpGet]
    public async Task<ActionResult<List<VentaResumenDto>>> Listar(
        [FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta, [FromQuery] int? usuarioId, CancellationToken ct)
    {
        var hoy = RangoFechas.HoyLocal();
        var filtroUsuario = User.EsAdministrador() ? usuarioId : User.UsuarioId();
        return Ok(await _ventas.ListarAsync(desde ?? hoy, hasta ?? hoy, filtroUsuario, ct));
    }

    /// <summary>Tiquete en PDF con el ancho del rollo térmico (58 u 80 mm) y alto continuo.</summary>
    [HttpGet("{id:int}/ticket/pdf")]
    [Produces("application/pdf")]
    public async Task<IActionResult> TicketPdf(int id, [FromQuery] int? ancho, CancellationToken ct)
    {
        var pdf = await _tickets.GenerarPdfAsync(id, SoloDelUsuario(), ancho, ct);
        return File(pdf, "application/pdf", $"tiquete-{id:D6}.pdf");
    }

    /// <summary>Bytes ESC/POS listos para enviar a la impresora térmica (por ejemplo desde un agente local).</summary>
    [HttpGet("{id:int}/ticket/escpos")]
    [Produces("application/octet-stream")]
    public async Task<IActionResult> TicketEscPos(int id, CancellationToken ct)
    {
        var datos = await _tickets.GenerarEscPosAsync(id, SoloDelUsuario(), ct);
        return File(datos, "application/octet-stream", $"tiquete-{id:D6}.bin");
    }

    /// <summary>Reimprime el tiquete en la impresora térmica configurada. No abre el cajón.</summary>
    [HttpPost("{id:int}/imprimir")]
    public async Task<IActionResult> Reimprimir(int id, CancellationToken ct)
    {
        await _tickets.ImprimirAsync(id, SoloDelUsuario(), esVentaNueva: false, ct);
        return NoContent();
    }

    private int? SoloDelUsuario() => User.EsAdministrador() ? null : User.UsuarioId();
}
