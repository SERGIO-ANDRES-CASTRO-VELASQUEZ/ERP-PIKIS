using ErpPikis.Api.Seguridad;
using ErpPikis.Domain.Entities;
using ErpPikis.Service.Common;
using ErpPikis.Service.Dtos;
using ErpPikis.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpPikis.Api.Controllers;

[ApiController]
[Route("api/caja")]
[Authorize]
public class CajaController : ControllerBase
{
    private readonly ICajaService _caja;

    public CajaController(ICajaService caja) => _caja = caja;

    [HttpPost("apertura")]
    public async Task<ActionResult<TurnoCajaDto>> Abrir(AbrirCajaRequest request, CancellationToken ct) =>
        Ok(await _caja.AbrirAsync(request, User.UsuarioId(), ct));

    [HttpGet("turno-actual")]
    public async Task<ActionResult<TurnoCajaDto>> TurnoActual(CancellationToken ct)
    {
        var turno = await _caja.ObtenerTurnoActualAsync(User.UsuarioId(), ct);
        return turno is null ? NoContent() : Ok(turno);
    }

    [HttpPost("egresos")]
    public async Task<ActionResult<TurnoCajaDto>> RegistrarEgreso(EgresoRequest request, CancellationToken ct) =>
        Ok(await _caja.RegistrarEgresoAsync(request, User.UsuarioId(), ct));

    [HttpPost("cierre")]
    public async Task<ActionResult<TurnoCajaDto>> Cerrar(CerrarCajaRequest request, CancellationToken ct) =>
        Ok(await _caja.CerrarAsync(request, User.UsuarioId(), ct));

    [HttpGet("turnos")]
    [Authorize(Roles = Rol.Administrador)]
    public async Task<ActionResult<List<TurnoCajaResumenDto>>> ListarTurnos(
        [FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta, CancellationToken ct)
    {
        var hoy = RangoFechas.HoyLocal();
        return Ok(await _caja.ListarTurnosAsync(desde ?? hoy.AddDays(-30), hasta ?? hoy, ct));
    }

    [HttpGet("turnos/{id:int}")]
    [Authorize(Roles = Rol.Administrador)]
    public async Task<ActionResult<TurnoCajaDto>> ObtenerTurno(int id, CancellationToken ct) =>
        Ok(await _caja.ObtenerTurnoAsync(id, ct));
}
