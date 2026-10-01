using ErpPikis.Domain.Entities;
using ErpPikis.Domain.Enums;
using ErpPikis.Domain.Exceptions;
using ErpPikis.Repository.Interfaces;
using ErpPikis.Service.Alertas;
using ErpPikis.Service.Common;
using ErpPikis.Service.Dtos;
using ErpPikis.Service.Exceptions;
using ErpPikis.Service.Interfaces;

namespace ErpPikis.Service.Services;

public class ProductoService : IProductoService
{
    private readonly IProductoRepository _productos;
    private readonly ICategoriaRepository _categorias;
    private readonly IUnitOfWork _uow;
    private readonly IAlertaService _alertas;

    public ProductoService(IProductoRepository productos, ICategoriaRepository categorias, IUnitOfWork uow, IAlertaService alertas)
    {
        _productos = productos;
        _categorias = categorias;
        _uow = uow;
        _alertas = alertas;
    }

    public async Task<List<ProductoDto>> ListarAsync(string? busqueda, bool soloBajoStock, CancellationToken ct = default) =>
        (await _productos.ListarAsync(busqueda, soloBajoStock, ct)).Select(p => p.ADto()).ToList();

    public async Task<ProductoDto> ObtenerAsync(int id, CancellationToken ct = default) =>
        (await ObtenerEntidadAsync(id, ct)).ADto();

    public async Task<ProductoDto> ObtenerPorCodigoBarrasAsync(string codigoBarras, CancellationToken ct = default)
    {
        var producto = await _productos.ObtenerPorCodigoBarrasAsync(codigoBarras.Trim(), ct)
            ?? throw new NoEncontradoException($"No existe un producto con código {codigoBarras}.");
        return producto.ADto();
    }

    public async Task<ProductoDto> CrearAsync(CrearProductoRequest request, int usuarioId, CancellationToken ct = default)
    {
        var codigo = NormalizarCodigo(request.CodigoBarras);
        if (codigo is not null && await _productos.ExisteCodigoBarrasAsync(codigo, null, ct))
            throw new DomainException($"Ya existe un producto con el código {codigo}.");

        var categoria = await _categorias.ObtenerPorIdAsync(request.CategoriaId, ct)
            ?? throw new NoEncontradoException("Categoría no encontrada.");

        var producto = new Producto
        {
            CodigoBarras = codigo,
            Nombre = request.Nombre.Trim(),
            CategoriaId = categoria.Id,
            Categoria = categoria,
            StockMinimo = request.StockMinimo
        };
        producto.FijarPrecios(request.PrecioCompra, request.PrecioVenta);
        _productos.Agregar(producto);

        if (request.StockInicial > 0)
        {
            producto.AgregarStock(request.StockInicial);
            _productos.AgregarMovimiento(new MovimientoInventario
            {
                Producto = producto,
                Tipo = TipoMovimientoInventario.Entrada,
                Cantidad = request.StockInicial,
                CostoUnitario = request.PrecioCompra,
                Motivo = "Stock inicial",
                UsuarioId = usuarioId
            });
        }

        await GuardarYEvaluarAsync(producto, ct);
        return producto.ADto();
    }

    public async Task<ProductoDto> ActualizarAsync(int id, ActualizarProductoRequest request, CancellationToken ct = default)
    {
        var producto = await ObtenerEntidadAsync(id, ct);

        var codigo = NormalizarCodigo(request.CodigoBarras);
        if (codigo is not null && await _productos.ExisteCodigoBarrasAsync(codigo, id, ct))
            throw new DomainException($"Ya existe otro producto con el código {codigo}.");

        if (producto.CategoriaId != request.CategoriaId)
        {
            producto.Categoria = await _categorias.ObtenerPorIdAsync(request.CategoriaId, ct)
                ?? throw new NoEncontradoException("Categoría no encontrada.");
            producto.CategoriaId = request.CategoriaId;
        }

        producto.CodigoBarras = codigo;
        producto.Nombre = request.Nombre.Trim();
        producto.StockMinimo = request.StockMinimo;
        producto.FijarPrecios(request.PrecioCompra, request.PrecioVenta);

        await GuardarYEvaluarAsync(producto, ct);
        return producto.ADto();
    }

    public async Task DesactivarAsync(int id, CancellationToken ct = default)
    {
        var producto = await ObtenerEntidadAsync(id, ct);
        producto.Activo = false;
        await GuardarYEvaluarAsync(producto, ct);
    }

    public async Task<ProductoDto> RegistrarEntradaAsync(int id, EntradaInventarioRequest request, int usuarioId, CancellationToken ct = default)
    {
        var producto = await ObtenerEntidadAsync(id, ct);

        if (request.CostoUnitario is { } costo && costo != producto.PrecioCompra)
            producto.FijarPrecios(costo, producto.PrecioVenta);

        producto.AgregarStock(request.Cantidad);
        _productos.AgregarMovimiento(new MovimientoInventario
        {
            ProductoId = producto.Id,
            Tipo = TipoMovimientoInventario.Entrada,
            Cantidad = request.Cantidad,
            CostoUnitario = producto.PrecioCompra,
            Motivo = string.IsNullOrWhiteSpace(request.Motivo) ? "Compra a proveedor" : request.Motivo.Trim(),
            UsuarioId = usuarioId
        });

        await GuardarYEvaluarAsync(producto, ct);
        return producto.ADto();
    }

    public async Task<ProductoDto> AjustarAsync(int id, AjusteInventarioRequest request, int usuarioId, CancellationToken ct = default)
    {
        if (request.Diferencia == 0)
            throw new DomainException("La diferencia del ajuste no puede ser cero.");

        var producto = await ObtenerEntidadAsync(id, ct);
        producto.AjustarStock(request.Diferencia);
        _productos.AgregarMovimiento(new MovimientoInventario
        {
            ProductoId = producto.Id,
            Tipo = TipoMovimientoInventario.Ajuste,
            Cantidad = request.Diferencia,
            CostoUnitario = producto.PrecioCompra,
            Motivo = request.Motivo.Trim(),
            UsuarioId = usuarioId
        });

        await GuardarYEvaluarAsync(producto, ct);
        return producto.ADto();
    }

    public async Task<List<MovimientoInventarioDto>> ListarMovimientosAsync(int id, CancellationToken ct = default)
    {
        await ObtenerEntidadAsync(id, ct);
        return (await _productos.ListarMovimientosAsync(id, ct)).Select(m => m.ADto()).ToList();
    }

    private async Task GuardarYEvaluarAsync(Producto producto, CancellationToken ct)
    {
        await _uow.GuardarCambiosAsync(ct);
        await _alertas.EvaluarProductosAsync([producto.Id], ct);
    }

    private async Task<Producto> ObtenerEntidadAsync(int id, CancellationToken ct) =>
        await _productos.ObtenerPorIdAsync(id, ct) ?? throw new NoEncontradoException("Producto no encontrado.");

    private static string? NormalizarCodigo(string? codigo) =>
        string.IsNullOrWhiteSpace(codigo) ? null : codigo.Trim();
}
