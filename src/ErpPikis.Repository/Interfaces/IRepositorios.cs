using ErpPikis.Domain.Entities;
using ErpPikis.Domain.Enums;

namespace ErpPikis.Repository.Interfaces;

public interface IUnitOfWork
{
    Task GuardarCambiosAsync(CancellationToken ct = default);
}

public interface IUsuarioRepository
{
    Task<Usuario?> ObtenerPorIdAsync(int id, CancellationToken ct = default);
    Task<Usuario?> ObtenerPorUsernameAsync(string username, CancellationToken ct = default);
    Task<List<Usuario>> ListarAsync(CancellationToken ct = default);
    Task<bool> ExisteUsernameAsync(string username, CancellationToken ct = default);
    Task<bool> HayUsuariosAsync(CancellationToken ct = default);
    Task<Rol?> ObtenerRolPorNombreAsync(string nombre, CancellationToken ct = default);
    void Agregar(Usuario usuario);
}

public interface ICategoriaRepository
{
    Task<List<Categoria>> ListarAsync(CancellationToken ct = default);
    Task<Categoria?> ObtenerPorIdAsync(int id, CancellationToken ct = default);
    Task<bool> ExisteNombreAsync(string nombre, CancellationToken ct = default);
    void Agregar(Categoria categoria);
}

public interface IProductoRepository
{
    Task<List<Producto>> ListarAsync(string? busqueda, bool soloBajoStock, CancellationToken ct = default);
    Task<Producto?> ObtenerPorIdAsync(int id, CancellationToken ct = default);
    Task<Producto?> ObtenerPorCodigoBarrasAsync(string codigoBarras, CancellationToken ct = default);
    Task<List<Producto>> ObtenerPorIdsAsync(IEnumerable<int> ids, CancellationToken ct = default);
    Task<List<int>> ListarIdsAsync(CancellationToken ct = default);
    Task<bool> ExisteCodigoBarrasAsync(string codigoBarras, int? excluirId, CancellationToken ct = default);
    Task<decimal> ValorInventarioAsync(CancellationToken ct = default);
    Task<List<MovimientoInventario>> ListarMovimientosAsync(int productoId, CancellationToken ct = default);
    void Agregar(Producto producto);
    void AgregarMovimiento(MovimientoInventario movimiento);
}

public interface IClienteRepository
{
    Task<List<Cliente>> ListarAsync(string? busqueda, CancellationToken ct = default);
    Task<Cliente?> ObtenerPorIdAsync(int id, CancellationToken ct = default);
    Task<decimal> TotalCarteraAsync(CancellationToken ct = default);
    Task<List<Cliente>> ObtenerPorIdsAsync(IEnumerable<int> ids, CancellationToken ct = default);

    /// <summary>
    /// Clientes con deuda y la fecha desde la que no abonan (último abono o, si nunca abonó, su primer fiado).
    /// </summary>
    Task<List<(Cliente Cliente, DateTime? ReferenciaUtc)>> ListarDeudoresAsync(IEnumerable<int>? ids, CancellationToken ct = default);
    void Agregar(Cliente cliente);
}

public interface IVentaRepository
{
    Task<Venta?> ObtenerPorIdAsync(int id, CancellationToken ct = default);
    Task<List<Venta>> ListarAsync(DateTime desdeUtc, DateTime hastaUtc, int? usuarioId, CancellationToken ct = default);
    Task<decimal> TotalPorTipoPagoEnTurnoAsync(int turnoId, TipoPago tipoPago, CancellationToken ct = default);
    Task<Dictionary<TipoPago, decimal>> TotalesPorTipoPagoAsync(DateTime desdeUtc, DateTime hastaUtc, CancellationToken ct = default);
    Task<decimal> UtilidadBrutaAsync(DateTime desdeUtc, DateTime hastaUtc, CancellationToken ct = default);
    Task<List<(DateTime FechaUtc, decimal Total)>> TotalesPorVentaAsync(DateTime desdeUtc, DateTime hastaUtc, CancellationToken ct = default);
    Task<List<(int ProductoId, string Nombre, int Cantidad, decimal Total)>> MasVendidosAsync(DateTime desdeUtc, DateTime hastaUtc, int top, CancellationToken ct = default);
    void Agregar(Venta venta);
}

public interface ICajaRepository
{
    Task<TurnoCaja?> ObtenerTurnoAbiertoAsync(int usuarioId, CancellationToken ct = default);
    Task<TurnoCaja?> ObtenerTurnoPorIdAsync(int id, CancellationToken ct = default);
    Task<List<TurnoCaja>> ListarTurnosAbiertosAsync(CancellationToken ct = default);
    Task<List<TurnoCaja>> ListarTurnosAsync(DateTime desdeUtc, DateTime hastaUtc, CancellationToken ct = default);
    Task<decimal> SumarMovimientosAsync(int turnoId, TipoMovimientoCaja tipo, CancellationToken ct = default);
    Task<decimal> SumarMovimientosAsync(DateTime desdeUtc, DateTime hastaUtc, TipoMovimientoCaja tipo, CancellationToken ct = default);
    Task<List<MovimientoCaja>> ListarMovimientosAsync(int turnoId, CancellationToken ct = default);
    void AgregarTurno(TurnoCaja turno);
    void AgregarMovimiento(MovimientoCaja movimiento);
}
