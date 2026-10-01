namespace ErpPikis.Service.Dtos;

public record VentasPorDiaDto(DateOnly Dia, decimal Total, int NumeroVentas);

public record ProductoMasVendidoDto(int ProductoId, string Nombre, int Cantidad, decimal Total);

public record AlertaStockDto(int ProductoId, string Nombre, int StockActual, int StockMinimo);

public record DashboardDto(
    DateOnly Fecha,
    decimal VentasDelDia,
    int NumeroVentasDelDia,
    decimal VentasContado,
    decimal VentasCredito,
    decimal AbonosCartera,
    decimal EgresosDelDia,
    decimal UtilidadBrutaDelDia,
    decimal CarteraTotal,
    decimal ValorInventario,
    List<AlertaStockDto> AlertasStock,
    List<VentasPorDiaDto> VentasUltimos7Dias,
    List<ProductoMasVendidoDto> MasVendidos7Dias);
