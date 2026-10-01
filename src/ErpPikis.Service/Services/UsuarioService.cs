using ErpPikis.Domain.Entities;
using ErpPikis.Domain.Exceptions;
using ErpPikis.Repository.Interfaces;
using ErpPikis.Service.Common;
using ErpPikis.Service.Dtos;
using ErpPikis.Service.Exceptions;
using ErpPikis.Service.Interfaces;

namespace ErpPikis.Service.Services;

public class UsuarioService : IUsuarioService
{
    private readonly IUsuarioRepository _usuarios;
    private readonly IUnitOfWork _uow;

    public UsuarioService(IUsuarioRepository usuarios, IUnitOfWork uow)
    {
        _usuarios = usuarios;
        _uow = uow;
    }

    public async Task<List<UsuarioDto>> ListarAsync(CancellationToken ct = default) =>
        (await _usuarios.ListarAsync(ct)).Select(u => u.ADto()).ToList();

    public async Task<UsuarioDto> CrearAsync(CrearUsuarioRequest request, CancellationToken ct = default)
    {
        var username = request.Username.Trim().ToLowerInvariant();

        if (await _usuarios.ExisteUsernameAsync(username, ct))
            throw new DomainException($"El usuario '{username}' ya existe.");

        var rol = await _usuarios.ObtenerRolPorNombreAsync(request.Rol, ct)
            ?? throw new DomainException($"Rol inválido. Use '{Rol.Administrador}' o '{Rol.Cajero}'.");

        var usuario = new Usuario
        {
            Nombre = request.Nombre.Trim(),
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            RolId = rol.Id,
            Rol = rol
        };

        _usuarios.Agregar(usuario);
        await _uow.GuardarCambiosAsync(ct);
        return usuario.ADto();
    }

    public async Task CambiarEstadoAsync(int id, bool activo, int usuarioActualId, CancellationToken ct = default)
    {
        if (id == usuarioActualId && !activo)
            throw new DomainException("No puede desactivar su propio usuario.");

        var usuario = await _usuarios.ObtenerPorIdAsync(id, ct)
            ?? throw new NoEncontradoException("Usuario no encontrado.");

        usuario.Activo = activo;
        await _uow.GuardarCambiosAsync(ct);
    }
}
