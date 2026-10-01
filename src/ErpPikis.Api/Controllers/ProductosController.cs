using ErpPikis.Api.Seguridad;
using ErpPikis.Domain.Entities;
using ErpPikis.Service.Dtos;
using ErpPikis.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpPikis.Api.Controllers;

[ApiController]
[Route("api/productos")]
[Authorize]
public class ProductosController : ControllerBase
{
    private readonly IProductoService _productos;

    public ProductosController(IProductoService productos) => _productos = productos;

    [HttpGet]
    public async Task<ActionResult<List<ProductoDto>>> Listar(
        [FromQuery] string? busqueda, [FromQuery] bool soloBajoStock, CancellationToken ct) =>
        Ok(await _productos.ListarAsync(busqueda, soloBajoStock, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductoDto>> Obtener(int id, CancellationToken ct) =>
        Ok(await _productos.ObtenerAsync(id, ct));

    [HttpGet("codigo/{codigoBarras}")]
    public async Task<ActionResult<ProductoDto>> ObtenerPorCodigo(string codigoBarras, CancellationToken ct) =>
        Ok(await _productos.ObtenerPorCodigoBarrasAsync(codigoBarras, ct));

    [HttpPost]
    [Authorize(Roles = Rol.Administrador)]
    public async Task<ActionResult<ProductoDto>> Crear(CrearProductoRequest request, CancellationToken ct)
    {
        var producto = await _productos.CrearAsync(request, User.UsuarioId(), ct);
        return CreatedAtAction(nameof(Obtener), new { id = producto.Id }, producto);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Rol.Administrador)]
    public async Task<ActionResult<ProductoDto>> Actualizar(int id, ActualizarProductoRequest request, CancellationToken ct) =>
        Ok(await _productos.ActualizarAsync(id, request, ct));

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Rol.Administrador)]
    public async Task<IActionResult> Desactivar(int id, CancellationToken ct)
    {
        await _productos.DesactivarAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:int}/entradas")]
    [Authorize(Roles = Rol.Administrador)]
    public async Task<ActionResult<ProductoDto>> RegistrarEntrada(int id, EntradaInventarioRequest request, CancellationToken ct) =>
        Ok(await _productos.RegistrarEntradaAsync(id, request, User.UsuarioId(), ct));

    [HttpPost("{id:int}/ajustes")]
    [Authorize(Roles = Rol.Administrador)]
    public async Task<ActionResult<ProductoDto>> Ajustar(int id, AjusteInventarioRequest request, CancellationToken ct) =>
        Ok(await _productos.AjustarAsync(id, request, User.UsuarioId(), ct));

    [HttpGet("{id:int}/movimientos")]
    [Authorize(Roles = Rol.Administrador)]
    public async Task<ActionResult<List<MovimientoInventarioDto>>> Movimientos(int id, CancellationToken ct) =>
        Ok(await _productos.ListarMovimientosAsync(id, ct));
}
