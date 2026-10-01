using ErpPikis.Domain.Exceptions;

namespace ErpPikis.Domain.Entities;

public class TurnoCaja
{
    public int Id { get; set; }
    public DateTime FechaApertura { get; set; } = DateTime.UtcNow;
    public decimal MontoInicial { get; set; }
    public DateTime? FechaCierre { get; set; }
    public decimal? MontoEsperado { get; set; }
    public decimal? MontoContado { get; set; }
    public decimal? Diferencia { get; set; }

    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    public ICollection<MovimientoCaja> Movimientos { get; set; } = new List<MovimientoCaja>();
    public ICollection<Venta> Ventas { get; set; } = new List<Venta>();

    public bool Abierto => FechaCierre is null;

    public void Cerrar(decimal montoEsperado, decimal montoContado)
    {
        if (!Abierto)
            throw new DomainException("El turno ya está cerrado.");
        if (montoContado < 0)
            throw new DomainException("El efectivo contado no puede ser negativo.");

        FechaCierre = DateTime.UtcNow;
        MontoEsperado = montoEsperado;
        MontoContado = montoContado;
        Diferencia = montoContado - montoEsperado;
    }
}
