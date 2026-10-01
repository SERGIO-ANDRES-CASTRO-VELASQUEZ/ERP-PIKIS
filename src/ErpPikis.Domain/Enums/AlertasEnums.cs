namespace ErpPikis.Domain.Enums;

public enum TipoAlerta
{
    StockBajo = 1,
    ProductoAgotado = 2,
    MargenBajo = 3,
    CreditoCercaDelLimite = 4,
    CarteraSinAbono = 5,
    DescuadreCaja = 6,
    TurnoAbiertoProlongado = 7,
    ResumenDiario = 8
}

public enum SeveridadAlerta
{
    Info = 1,
    Advertencia = 2,
    Critica = 3
}

public enum EstadoNotificacion
{
    Pendiente = 1,
    Enviada = 2,
    Fallida = 3
}
