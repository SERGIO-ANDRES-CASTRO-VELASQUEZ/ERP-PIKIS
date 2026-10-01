using ErpPikis.Api.Seguridad;
using ErpPikis.Service.Dtos;
using ErpPikis.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpPikis.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken ct) =>
        Ok(await _auth.LoginAsync(request, ct));

    [Authorize]
    [HttpGet("perfil")]
    public async Task<ActionResult<UsuarioDto>> Perfil(CancellationToken ct) =>
        Ok(await _auth.ObtenerPerfilAsync(User.UsuarioId(), ct));

    [Authorize]
    [HttpPost("cambiar-password")]
    public async Task<IActionResult> CambiarPassword(CambiarPasswordRequest request, CancellationToken ct)
    {
        await _auth.CambiarPasswordAsync(User.UsuarioId(), request, ct);
        return NoContent();
    }
}
