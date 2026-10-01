using ErpPikis.Domain.Entities;
using ErpPikis.Service.Dtos;
using ErpPikis.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpPikis.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Roles = Rol.Administrador)]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboard;

    public DashboardController(IDashboardService dashboard) => _dashboard = dashboard;

    [HttpGet]
    public async Task<ActionResult<DashboardDto>> Obtener([FromQuery] DateOnly? fecha, CancellationToken ct) =>
        Ok(await _dashboard.ObtenerAsync(fecha, ct));
}
