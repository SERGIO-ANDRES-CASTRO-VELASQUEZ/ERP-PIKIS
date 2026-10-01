using System.ComponentModel.DataAnnotations;

namespace ErpPikis.Service.Dtos;

public record CategoriaDto(int Id, string Nombre);

public record CrearCategoriaRequest([Required, StringLength(80)] string Nombre);

public record ProductoDto(
    int Id,
    string? CodigoBarras,
    string Nombre,
    int CategoriaId,
    string Categoria,
    decimal PrecioCompra,
    decimal PrecioVenta,
    decimal MargenPorcentaje,
    int StockActual,
    int StockMinimo,
    bool RequiereReabastecimiento);

public record CrearProductoRequest(
    [StringLength(50)] string? CodigoBarras,
    [Required, StringLength(150)] string Nombre,
    [Range(1, int.MaxValue)] int CategoriaId,
    [Range(0, 99_999_999)] decimal PrecioCompra,
    [Range(0, 99_999_999)] decimal PrecioVenta,
    [Range(0, int.MaxValue)] int StockInicial,
    [Range(0, int.MaxValue)] int StockMinimo);

public record ActualizarProductoRequest(
    [StringLength(50)] string? CodigoBarras,
    [Required, StringLength(150)] string Nombre,
    [Range(1, int.MaxValue)] int CategoriaId,
    [Range(0, 99_999_999)] decimal PrecioCompra,
    [Range(0, 99_999_999)] decimal PrecioVenta,
    [Range(0, int.MaxValue)] int StockMinimo);

public record EntradaInventarioRequest(
    [Range(1, int.MaxValue)] int Cantidad,
    [Range(0, 99_999_999)] decimal? CostoUnitario,
    [StringLength(250)] string? Motivo);

public record AjusteInventarioRequest(
    int Diferencia,
    [Required, StringLength(250)] string Motivo);

public record MovimientoInventarioDto(
    int Id,
    DateTime Fecha,
    string Tipo,
    int Cantidad,
    decimal? CostoUnitario,
    string? Motivo,
    string Usuario);
