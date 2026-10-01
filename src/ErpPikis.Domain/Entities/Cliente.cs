using ErpPikis.Domain.Exceptions;

namespace ErpPikis.Domain.Entities;

public class Cliente
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public decimal LimiteCredito { get; set; }
    public decimal SaldoActual { get; set; }
    public bool Activo { get; set; } = true;

    public decimal CreditoDisponible => LimiteCredito - SaldoActual;

    public void CargarCredito(decimal monto)
    {
        if (monto <= 0)
            throw new DomainException("El monto a fiar debe ser mayor que cero.");
        if (!Activo)
            throw new DomainException($"El cliente {Nombre} está inactivo.");
        if (monto > CreditoDisponible)
            throw new DomainException(
                $"El cliente {Nombre} supera su límite de crédito. Disponible: {CreditoDisponible:N0}.");

        SaldoActual += monto;
    }

    public void Abonar(decimal monto)
    {
        if (monto <= 0)
            throw new DomainException("El abono debe ser mayor que cero.");
        if (monto > SaldoActual)
            throw new DomainException($"El abono supera la deuda actual ({SaldoActual:N0}).");

        SaldoActual -= monto;
    }
}
