using ErpPikis.Domain.Entities;
using ErpPikis.Repository.Interfaces;
using ErpPikis.Service.Interfaces;

namespace ErpPikis.Service.Services;

public class DatosInicialesService : IDatosInicialesService
{
    private readonly IUsuarioRepository _usuarios;
    private readonly IUnitOfWork _uow;

    public DatosInicialesService(IUsuarioRepository usuarios, IUnitOfWork uow)
    {
        _usuarios = usuarios;
        _uow = uow;
    }

    public async Task SembrarAsync(string usernameAdmin, string passwordAdmin, CancellationToken ct = default)
    {
        if (await _usuarios.HayUsuariosAsync(ct))
            return;

        var rol = await _usuarios.ObtenerRolPorNombreAsync(Rol.Administrador, ct)
            ?? throw new InvalidOperationException("No existe el rol Administrador. ¿Se aplicaron las migraciones?");

        _usuarios.Agregar(new Usuario
        {
            Nombre = "Propietaria",
            Username = usernameAdmin.Trim().ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(passwordAdmin),
            RolId = rol.Id
        });

        await _uow.GuardarCambiosAsync(ct);
    }
}
