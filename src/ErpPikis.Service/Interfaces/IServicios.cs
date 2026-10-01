using ErpPikis.Domain.Entities;
using ErpPikis.Service.Dtos;

namespace ErpPikis.Service.Interfaces;

public interface ITokenGenerator
{
    (string Token, DateTime ExpiraUtc) Generar(Usuario usuario);
}

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<UsuarioDto> ObtenerPerfilAsync(int usuarioId, CancellationToken ct = default);
    Task CambiarPasswordAsync(int usuarioId, CambiarPasswordRequest request, CancellationToken ct = default);
}

public interface IUsuarioService
{
    Task<List<UsuarioDto>> ListarAsync(CancellationToken ct = default);
    Task<UsuarioDto> CrearAsync(CrearUsuarioRequest request, CancellationToken ct = default);
    Task CambiarEstadoAsync(int id, bool activo, int usuarioActualId, CancellationToken ct = default);
}

public interface ICategoriaService
{
    Task<List<CategoriaDto>> ListarAsync(CancellationToken ct = default);
    Task<CategoriaDto> CrearAsync(CrearCategoriaRequest request, CancellationToken ct = default);
}

public interface IProductoService
{
    Task<List<ProductoDto>> ListarAsync(string? busqueda, bool soloBajoStock, CancellationToken ct = default);
    Task<ProductoDto> ObtenerAsync(int id, CancellationToken ct = default);
    Task<ProductoDto> ObtenerPorCodigoBarrasAsync(string codigoBarras, CancellationToken ct = default);
    Task<ProductoDto> CrearAsync(CrearProductoRequest request, int usuarioId, CancellationToken ct = default);
    Task<ProductoDto> ActualizarAsync(int id, ActualizarProductoRequest request, CancellationToken ct = default);
    Task DesactivarAsync(int id, CancellationToken ct = default);
    Task<ProductoDto> RegistrarEntradaAsync(int id, EntradaInventarioRequest request, int usuarioId, CancellationToken ct = default);
    Task<ProductoDto> AjustarAsync(int id, AjusteInventarioRequest request, int usuarioId, CancellationToken ct = default);
    Task<List<MovimientoInventarioDto>> ListarMovimientosAsync(int id, CancellationToken ct = default);
}

public interface IClienteService
{
    Task<List<ClienteDto>> ListarAsync(string? busqueda, CancellationToken ct = default);
    Task<ClienteDto> ObtenerAsync(int id, CancellationToken ct = default);
    Task<ClienteDto> CrearAsync(CrearClienteRequest request, CancellationToken ct = default);
    Task<ClienteDto> ActualizarAsync(int id, ActualizarClienteRequest request, CancellationToken ct = default);
    Task<ClienteDto> AbonarAsync(int id, AbonoRequest request, int usuarioId, CancellationToken ct = default);
}

public interface IVentaService
{
    Task<VentaDto> RegistrarAsync(RegistrarVentaRequest request, int usuarioId, CancellationToken ct = default);
    Task<VentaDto> ObtenerAsync(int id, int? soloDelUsuarioId, CancellationToken ct = default);
    Task<List<VentaResumenDto>> ListarAsync(DateOnly desde, DateOnly hasta, int? usuarioId, CancellationToken ct = default);
}

public interface ICajaService
{
    Task<TurnoCajaDto> AbrirAsync(AbrirCajaRequest request, int usuarioId, CancellationToken ct = default);
    Task<TurnoCajaDto?> ObtenerTurnoActualAsync(int usuarioId, CancellationToken ct = default);
    Task<TurnoCajaDto> RegistrarEgresoAsync(EgresoRequest request, int usuarioId, CancellationToken ct = default);
    Task<TurnoCajaDto> CerrarAsync(CerrarCajaRequest request, int usuarioId, CancellationToken ct = default);
    Task<TurnoCajaDto> ObtenerTurnoAsync(int turnoId, CancellationToken ct = default);
    Task<List<TurnoCajaResumenDto>> ListarTurnosAsync(DateOnly desde, DateOnly hasta, CancellationToken ct = default);
}

public interface IDashboardService
{
    Task<DashboardDto> ObtenerAsync(DateOnly? fecha, CancellationToken ct = default);
}

public interface IDatosInicialesService
{
    Task SembrarAsync(string usernameAdmin, string passwordAdmin, CancellationToken ct = default);
}
