using System.ComponentModel.DataAnnotations;

namespace ErpPikis.Service.Dtos;

public record ClienteDto(
    int Id,
    string Nombre,
    string? Telefono,
    decimal LimiteCredito,
    decimal SaldoActual,
    decimal CreditoDisponible);

public record CrearClienteRequest(
    [Required, StringLength(120)] string Nombre,
    [StringLength(20)] string? Telefono,
    [Range(0, 99_999_999)] decimal LimiteCredito);

public record ActualizarClienteRequest(
    [Required, StringLength(120)] string Nombre,
    [StringLength(20)] string? Telefono,
    [Range(0, 99_999_999)] decimal LimiteCredito);

public record AbonoRequest(
    [Range(1, 99_999_999)] decimal Monto,
    [StringLength(250)] string? Nota);
