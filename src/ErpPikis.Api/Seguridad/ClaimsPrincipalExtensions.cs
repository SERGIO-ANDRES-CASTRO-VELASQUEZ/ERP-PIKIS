using System.Security.Claims;
using ErpPikis.Domain.Entities;

namespace ErpPikis.Api.Seguridad;

public static class ClaimsPrincipalExtensions
{
    public static int UsuarioId(this ClaimsPrincipal user) =>
        int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? throw new UnauthorizedAccessException("Token sin identificador de usuario."));

    public static bool EsAdministrador(this ClaimsPrincipal user) => user.IsInRole(Rol.Administrador);
}
