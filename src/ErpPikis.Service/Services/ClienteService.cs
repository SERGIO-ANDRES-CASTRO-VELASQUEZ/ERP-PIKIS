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

public class ClienteService : IClienteService
{
    private readonly IClienteRepository _clientes;
    private readonly ICajaRepository _caja;
    private readonly IUnitOfWork _uow;
    private readonly IAlertaService _alertas;

    public ClienteService(IClienteRepository clientes, ICajaRepository caja, IUnitOfWork uow, IAlertaService alertas)
    {
        _clientes = clientes;
        _caja = caja;
        _uow = uow;
        _alertas = alertas;
    }

    public async Task<List<ClienteDto>> ListarAsync(string? busqueda, CancellationToken ct = default) =>
        (await _clientes.ListarAsync(busqueda, ct)).Select(c => c.ADto()).ToList();

    public async Task<ClienteDto> ObtenerAsync(int id, CancellationToken ct = default) =>
        (await ObtenerEntidadAsync(id, ct)).ADto();

    public async Task<ClienteDto> CrearAsync(CrearClienteRequest request, CancellationToken ct = default)
    {
        var cliente = new Cliente
        {
            Nombre = request.Nombre.Trim(),
            Telefono = string.IsNullOrWhiteSpace(request.Telefono) ? null : request.Telefono.Trim(),
            LimiteCredito = request.LimiteCredito
        };

        _clientes.Agregar(cliente);
        await _uow.GuardarCambiosAsync(ct);
        return cliente.ADto();
    }

    public async Task<ClienteDto> ActualizarAsync(int id, ActualizarClienteRequest request, CancellationToken ct = default)
    {
        var cliente = await ObtenerEntidadAsync(id, ct);

        if (request.LimiteCredito < cliente.SaldoActual)
            throw new DomainException(
                $"El nuevo límite ({request.LimiteCredito:N0}) es menor que la deuda actual ({cliente.SaldoActual:N0}).");

        cliente.Nombre = request.Nombre.Trim();
        cliente.Telefono = string.IsNullOrWhiteSpace(request.Telefono) ? null : request.Telefono.Trim();
        cliente.LimiteCredito = request.LimiteCredito;

        await _uow.GuardarCambiosAsync(ct);
        await _alertas.EvaluarClientesAsync([cliente.Id], ct);
        return cliente.ADto();
    }

    public async Task<ClienteDto> AbonarAsync(int id, AbonoRequest request, int usuarioId, CancellationToken ct = default)
    {
        var turno = await _caja.ObtenerTurnoAbiertoAsync(usuarioId, ct)
            ?? throw new DomainException("Debe abrir caja antes de recibir abonos.");

        var cliente = await ObtenerEntidadAsync(id, ct);
        cliente.Abonar(request.Monto);

        _caja.AgregarMovimiento(new MovimientoCaja
        {
            TurnoCajaId = turno.Id,
            UsuarioId = usuarioId,
            ClienteId = cliente.Id,
            Tipo = TipoMovimientoCaja.Ingreso,
            Monto = request.Monto,
            Descripcion = string.IsNullOrWhiteSpace(request.Nota)
                ? $"Abono de {cliente.Nombre}"
                : $"Abono de {cliente.Nombre}: {request.Nota.Trim()}"
        });

        await _uow.GuardarCambiosAsync(ct);
        await _alertas.EvaluarClientesAsync([cliente.Id], ct);
        return cliente.ADto();
    }

    private async Task<Cliente> ObtenerEntidadAsync(int id, CancellationToken ct) =>
        await _clientes.ObtenerPorIdAsync(id, ct) ?? throw new NoEncontradoException("Cliente no encontrado.");
}
