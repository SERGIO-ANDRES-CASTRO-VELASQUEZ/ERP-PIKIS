using System.ComponentModel.DataAnnotations;

namespace ErpPikis.Service.Dtos;

public record LoginRequest(
    [Required] string Username,
    [Required] string Password);

public record LoginResponse(string Token, DateTime ExpiraUtc, UsuarioDto Usuario);

public record UsuarioDto(int Id, string Nombre, string Username, string Rol, bool Activo);

public record CrearUsuarioRequest(
    [Required, StringLength(120)] string Nombre,
    [Required, StringLength(50, MinimumLength = 3)] string Username,
    [Required, StringLength(100, MinimumLength = 6)] string Password,
    [Required] string Rol);

public record CambiarPasswordRequest(
    [Required] string PasswordActual,
    [Required, StringLength(100, MinimumLength = 6)] string PasswordNueva);
