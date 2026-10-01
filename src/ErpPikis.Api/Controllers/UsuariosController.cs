using ErpPikis.Api.Seguridad;
using ErpPikis.Domain.Entities;
using ErpPikis.Service.Dtos;
using ErpPikis.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpPikis.Api.Controllers;

[ApiController]
[Route("api/usuarios")]
[Authorize(Roles = Rol.Administrador)]
public class UsuariosController : ControllerBase
{
    private readonly IUsuarioService _usuarios;

    public UsuariosController(IUsuarioService usuarios) => _usuarios = usuarios;

    [HttpGet]
    public async Task<ActionResult<List<UsuarioDto>>> Listar(CancellationToken ct) =>
        Ok(await _usuarios.ListarAsync(ct));

    [HttpPost]
    public async Task<ActionResult<UsuarioDto>> Crear(CrearUsuarioRequest request, CancellationToken ct)
    {
        var usuario = await _usuarios.CrearAsync(request, ct);
        return CreatedAtAction(nameof(Listar), null, usuario);
    }

    [HttpPut("{id:int}/activar")]
    public async Task<IActionResult> Activar(int id, CancellationToken ct)
    {
        await _usuarios.CambiarEstadoAsync(id, true, User.UsuarioId(), ct);
        return NoContent();
    }

    [HttpPut("{id:int}/desactivar")]
    public async Task<IActionResult> Desactivar(int id, CancellationToken ct)
    {
        await _usuarios.CambiarEstadoAsync(id, false, User.UsuarioId(), ct);
        return NoContent();
    }
}
