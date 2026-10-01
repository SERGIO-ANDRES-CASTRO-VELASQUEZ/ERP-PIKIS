using System.ComponentModel.DataAnnotations;

namespace ErpPikis.Service.Dtos;

public record AbrirCajaRequest([Range(0, 99_999_999)] decimal MontoInicial);

public record CerrarCajaRequest([Range(0, 99_999_999)] decimal MontoContado);

public record EgresoRequest(
    [Range(1, 99_999_999)] decimal Monto,
    [Required, StringLength(250)] string Descripcion);

public record MovimientoCajaDto(
    int Id,
    DateTime Fecha,
    string Tipo,
    decimal Monto,
    string Descripcion,
    string? Cliente);

public record TurnoCajaDto(
    int Id,
    string Cajero,
    DateTime FechaApertura,
    DateTime? FechaCierre,
    bool Abierto,
    decimal MontoInicial,
    decimal VentasEfectivo,
    decimal VentasNequi,
    decimal VentasDaviplata,
    decimal VentasCredito,
    decimal Ingresos,
    decimal Egresos,
    decimal EfectivoEsperado,
    decimal? MontoContado,
    decimal? Diferencia,
    List<MovimientoCajaDto> Movimientos);

public record TurnoCajaResumenDto(
    int Id,
    string Cajero,
    DateTime FechaApertura,
    DateTime? FechaCierre,
    decimal MontoInicial,
    decimal? MontoEsperado,
    decimal? MontoContado,
    decimal? Diferencia);
