using ErpPikis.Domain.Exceptions;
using ErpPikis.Repository.Interfaces;
using ErpPikis.Service.Common;
using ErpPikis.Service.Dtos;
using ErpPikis.Service.Exceptions;
using ErpPikis.Service.Interfaces;

namespace ErpPikis.Service.Services;

public class AuthService : IAuthService
{
    private readonly IUsuarioRepository _usuarios;
    private readonly IUnitOfWork _uow;
    private readonly ITokenGenerator _tokens;

    public AuthService(IUsuarioRepository usuarios, IUnitOfWork uow, ITokenGenerator tokens)
    {
        _usuarios = usuarios;
        _uow = uow;
        _tokens = tokens;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var usuario = await _usuarios.ObtenerPorUsernameAsync(request.Username.Trim(), ct);

        if (usuario is null || !usuario.Activo || !BCrypt.Net.BCrypt.Verify(request.Password, usuario.PasswordHash))
            throw new CredencialesInvalidasException();

        var (token, expira) = _tokens.Generar(usuario);
        return new LoginResponse(token, expira, usuario.ADto());
    }

    public async Task<UsuarioDto> ObtenerPerfilAsync(int usuarioId, CancellationToken ct = default)
    {
        var usuario = await _usuarios.ObtenerPorIdAsync(usuarioId, ct)
            ?? throw new NoEncontradoException("Usuario no encontrado.");
        return usuario.ADto();
    }

    public async Task CambiarPasswordAsync(int usuarioId, CambiarPasswordRequest request, CancellationToken ct = default)
    {
        var usuario = await _usuarios.ObtenerPorIdAsync(usuarioId, ct)
            ?? throw new NoEncontradoException("Usuario no encontrado.");

        if (!BCrypt.Net.BCrypt.Verify(request.PasswordActual, usuario.PasswordHash))
            throw new DomainException("La contraseña actual no es correcta.");

        usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.PasswordNueva);
        await _uow.GuardarCambiosAsync(ct);
    }
}
