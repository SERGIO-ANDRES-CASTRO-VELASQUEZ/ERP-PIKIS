using ErpPikis.Domain.Entities;
using ErpPikis.Service.Dtos;
using ErpPikis.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpPikis.Api.Controllers;

[ApiController]
[Route("api/categorias")]
[Authorize]
public class CategoriasController : ControllerBase
{
    private readonly ICategoriaService _categorias;

    public CategoriasController(ICategoriaService categorias) => _categorias = categorias;

    [HttpGet]
    public async Task<ActionResult<List<CategoriaDto>>> Listar(CancellationToken ct) =>
        Ok(await _categorias.ListarAsync(ct));

    [HttpPost]
    [Authorize(Roles = Rol.Administrador)]
    public async Task<ActionResult<CategoriaDto>> Crear(CrearCategoriaRequest request, CancellationToken ct)
    {
        var categoria = await _categorias.CrearAsync(request, ct);
        return CreatedAtAction(nameof(Listar), null, categoria);
    }
}
