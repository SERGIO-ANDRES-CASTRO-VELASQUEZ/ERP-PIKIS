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

public class CajaService : ICajaService
{
    private readonly ICajaRepository _caja;
    private readonly IVentaRepository _ventas;
    private readonly IUnitOfWork _uow;
    private readonly IAlertaService _alertas;

    public CajaService(ICajaRepository caja, IVentaRepository ventas, IUnitOfWork uow, IAlertaService alertas)
    {
        _caja = caja;
        _ventas = ventas;
        _uow = uow;
        _alertas = alertas;
    }

    public async Task<TurnoCajaDto> AbrirAsync(AbrirCajaRequest request, int usuarioId, CancellationToken ct = default)
    {
        if (await _caja.ObtenerTurnoAbiertoAsync(usuarioId, ct) is not null)
            throw new DomainException("Ya tiene un turno de caja abierto.");

        var turno = new TurnoCaja { UsuarioId = usuarioId, MontoInicial = request.MontoInicial };
        _caja.AgregarTurno(turno);
        _caja.AgregarMovimiento(new MovimientoCaja
        {
            TurnoCaja = turno,
            UsuarioId = usuarioId,
            Tipo = TipoMovimientoCaja.Apertura,
            Monto = request.MontoInicial,
            Descripcion = "Apertura de caja (base)"
        });

        await _uow.GuardarCambiosAsync(ct);
        return await ConstruirDtoAsync(turno.Id, ct);
    }

    public async Task<TurnoCajaDto?> ObtenerTurnoActualAsync(int usuarioId, CancellationToken ct = default)
    {
        var turno = await _caja.ObtenerTurnoAbiertoAsync(usuarioId, ct);
        return turno is null ? null : await ConstruirDtoAsync(turno.Id, ct);
    }

    public async Task<TurnoCajaDto> RegistrarEgresoAsync(EgresoRequest request, int usuarioId, CancellationToken ct = default)
    {
        var turno = await ObtenerTurnoAbiertoObligatorioAsync(usuarioId, ct);
        var efectivoDisponible = await CalcularEfectivoEsperadoAsync(turno, ct);

        if (request.Monto > efectivoDisponible)
            throw new DomainException($"No hay suficiente efectivo en caja. Disponible: {efectivoDisponible:N0}.");

        _caja.AgregarMovimiento(new MovimientoCaja
        {
            TurnoCajaId = turno.Id,
            UsuarioId = usuarioId,
            Tipo = TipoMovimientoCaja.Egreso,
            Monto = request.Monto,
            Descripcion = request.Descripcion.Trim()
        });

        await _uow.GuardarCambiosAsync(ct);
        return await ConstruirDtoAsync(turno.Id, ct);
    }

    public async Task<TurnoCajaDto> CerrarAsync(CerrarCajaRequest request, int usuarioId, CancellationToken ct = default)
    {
        var turno = await ObtenerTurnoAbiertoObligatorioAsync(usuarioId, ct);
        var esperado = await CalcularEfectivoEsperadoAsync(turno, ct);

        turno.Cerrar(esperado, request.MontoContado);
        _caja.AgregarMovimiento(new MovimientoCaja
        {
            TurnoCajaId = turno.Id,
            UsuarioId = usuarioId,
            Tipo = TipoMovimientoCaja.Cierre,
            Monto = request.MontoContado,
            Descripcion = $"Cierre de caja. Esperado {esperado:N0}, contado {request.MontoContado:N0}, diferencia {turno.Diferencia:N0}"
        });

        await _uow.GuardarCambiosAsync(ct);
        await _alertas.EvaluarCierreCajaAsync(turno.Id, ct);
        return await ConstruirDtoAsync(turno.Id, ct);
    }

    public Task<TurnoCajaDto> ObtenerTurnoAsync(int turnoId, CancellationToken ct = default) =>
        ConstruirDtoAsync(turnoId, ct);

    public async Task<List<TurnoCajaResumenDto>> ListarTurnosAsync(DateOnly desde, DateOnly hasta, CancellationToken ct = default)
    {
        var (desdeUtc, hastaUtc) = RangoFechas.DiasLocalesEnUtc(desde, hasta);
        return (await _caja.ListarTurnosAsync(desdeUtc, hastaUtc, ct))
            .Select(t => new TurnoCajaResumenDto(
                t.Id, t.Usuario?.Nombre ?? string.Empty, t.FechaApertura, t.FechaCierre,
                t.MontoInicial, t.MontoEsperado, t.MontoContado, t.Diferencia))
            .ToList();
    }

    private async Task<TurnoCaja> ObtenerTurnoAbiertoObligatorioAsync(int usuarioId, CancellationToken ct) =>
        await _caja.ObtenerTurnoAbiertoAsync(usuarioId, ct)
        ?? throw new DomainException("No tiene un turno de caja abierto.");

    private async Task<decimal> CalcularEfectivoEsperadoAsync(TurnoCaja turno, CancellationToken ct)
    {
        var ventasEfectivo = await _ventas.TotalPorTipoPagoEnTurnoAsync(turno.Id, TipoPago.Efectivo, ct);
        var ingresos = await _caja.SumarMovimientosAsync(turno.Id, TipoMovimientoCaja.Ingreso, ct);
        var egresos = await _caja.SumarMovimientosAsync(turno.Id, TipoMovimientoCaja.Egreso, ct);
        return turno.MontoInicial + ventasEfectivo + ingresos - egresos;
    }

    private async Task<TurnoCajaDto> ConstruirDtoAsync(int turnoId, CancellationToken ct)
    {
        var turno = await _caja.ObtenerTurnoPorIdAsync(turnoId, ct)
            ?? throw new NoEncontradoException("Turno de caja no encontrado.");

        var efectivo = await _ventas.TotalPorTipoPagoEnTurnoAsync(turno.Id, TipoPago.Efectivo, ct);
        var nequi = await _ventas.TotalPorTipoPagoEnTurnoAsync(turno.Id, TipoPago.Nequi, ct);
        var daviplata = await _ventas.TotalPorTipoPagoEnTurnoAsync(turno.Id, TipoPago.Daviplata, ct);
        var credito = await _ventas.TotalPorTipoPagoEnTurnoAsync(turno.Id, TipoPago.Credito, ct);
        var ingresos = await _caja.SumarMovimientosAsync(turno.Id, TipoMovimientoCaja.Ingreso, ct);
        var egresos = await _caja.SumarMovimientosAsync(turno.Id, TipoMovimientoCaja.Egreso, ct);
        var movimientos = await _caja.ListarMovimientosAsync(turno.Id, ct);

        return new TurnoCajaDto(
            turno.Id,
            turno.Usuario?.Nombre ?? string.Empty,
            turno.FechaApertura,
            turno.FechaCierre,
            turno.Abierto,
            turno.MontoInicial,
            efectivo,
            nequi,
            daviplata,
            credito,
            ingresos,
            egresos,
            turno.MontoEsperado ?? turno.MontoInicial + efectivo + ingresos - egresos,
            turno.MontoContado,
            turno.Diferencia,
            movimientos.Select(m => m.ADto()).ToList());
    }
}
