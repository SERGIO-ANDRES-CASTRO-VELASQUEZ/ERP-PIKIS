using ErpPikis.Api.Seguridad;
using ErpPikis.Domain.Entities;
using ErpPikis.Service.Dtos;
using ErpPikis.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpPikis.Api.Controllers;

[ApiController]
[Route("api/clientes")]
[Authorize]
public class ClientesController : ControllerBase
{
    private readonly IClienteService _clientes;

    public ClientesController(IClienteService clientes) => _clientes = clientes;

    [HttpGet]
    public async Task<ActionResult<List<ClienteDto>>> Listar([FromQuery] string? busqueda, CancellationToken ct) =>
        Ok(await _clientes.ListarAsync(busqueda, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ClienteDto>> Obtener(int id, CancellationToken ct) =>
        Ok(await _clientes.ObtenerAsync(id, ct));

    [HttpPost]
    [Authorize(Roles = Rol.Administrador)]
    public async Task<ActionResult<ClienteDto>> Crear(CrearClienteRequest request, CancellationToken ct)
    {
        var cliente = await _clientes.CrearAsync(request, ct);
        return CreatedAtAction(nameof(Obtener), new { id = cliente.Id }, cliente);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Rol.Administrador)]
    public async Task<ActionResult<ClienteDto>> Actualizar(int id, ActualizarClienteRequest request, CancellationToken ct) =>
        Ok(await _clientes.ActualizarAsync(id, request, ct));

    [HttpPost("{id:int}/abonos")]
    public async Task<ActionResult<ClienteDto>> Abonar(int id, AbonoRequest request, CancellationToken ct) =>
        Ok(await _clientes.AbonarAsync(id, request, User.UsuarioId(), ct));
}
