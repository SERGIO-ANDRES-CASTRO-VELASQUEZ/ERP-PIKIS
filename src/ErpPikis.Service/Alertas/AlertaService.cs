using System.Globalization;
using ErpPikis.Domain.Entities;
using ErpPikis.Domain.Enums;
using ErpPikis.Repository.Interfaces;
using ErpPikis.Service.Common;
using ErpPikis.Service.Exceptions;
using ErpPikis.Service.Interfaces;
using Microsoft.Extensions.Logging;

namespace ErpPikis.Service.Alertas;

public interface IAlertaService
{
    Task EvaluarProductosAsync(IEnumerable<int> productoIds, CancellationToken ct = default);
    Task EvaluarClientesAsync(IEnumerable<int> clienteIds, CancellationToken ct = default);
    Task EvaluarCierreCajaAsync(int turnoId, CancellationToken ct = default);
    Task<int> RevisarTodoAsync(CancellationToken ct = default);

    Task<List<AlertaDto>> ListarAsync(bool soloNoLeidas, bool soloActivas, CancellationToken ct = default);
    Task<ResumenAlertasDto> ResumenAsync(CancellationToken ct = default);
    Task MarcarLeidaAsync(int id, CancellationToken ct = default);
    Task<int> MarcarTodasLeidasAsync(CancellationToken ct = default);
}

public class AlertaService : IAlertaService
{
    private const string Marca = "*ERP-PIKIS*";

    private readonly IAlertaRepository _alertas;
    private readonly IProductoRepository _productos;
    private readonly IClienteRepository _clientes;
    private readonly ICajaRepository _caja;
    private readonly IDashboardService _dashboard;
    private readonly IUnitOfWork _uow;
    private readonly IWhatsAppSender _whatsApp;
    private readonly AlertasOptions _opciones;
    private readonly ILogger<AlertaService> _logger;
    private int _creadas;

    public AlertaService(
        IAlertaRepository alertas,
        IProductoRepository productos,
        IClienteRepository clientes,
        ICajaRepository caja,
        IDashboardService dashboard,
        IUnitOfWork uow,
        IWhatsAppSender whatsApp,
        AlertasOptions opciones,
        ILogger<AlertaService> logger)
    {
        _alertas = alertas;
        _productos = productos;
        _clientes = clientes;
        _caja = caja;
        _dashboard = dashboard;
        _uow = uow;
        _whatsApp = whatsApp;
        _opciones = opciones;
        _logger = logger;
    }

    public Task EvaluarProductosAsync(IEnumerable<int> productoIds, CancellationToken ct = default) =>
        SinRomperAsync("productos", async () =>
        {
            await EvaluarProductosInternoAsync(productoIds.ToList(), ct);
            await _uow.GuardarCambiosAsync(ct);
        });

    public Task EvaluarClientesAsync(IEnumerable<int> clienteIds, CancellationToken ct = default) =>
        SinRomperAsync("clientes", async () =>
        {
            await EvaluarClientesInternoAsync(clienteIds.ToList(), ct);
            await _uow.GuardarCambiosAsync(ct);
        });

    public Task EvaluarCierreCajaAsync(int turnoId, CancellationToken ct = default) =>
        SinRomperAsync("cierre de caja", async () =>
        {
            var turno = await _caja.ObtenerTurnoPorIdAsync(turnoId, ct);
            if (turno is null || turno.Abierto)
                return;

            await ResolverAsync($"turno-abierto:{turno.Id}", ct);

            var diferencia = turno.Diferencia ?? 0;
            if (Math.Abs(diferencia) >= _opciones.UmbralDescuadreCaja)
            {
                var faltante = diferencia < 0;
                await RegistrarEventoAsync(
                    $"descuadre:{turno.Id}",
                    TipoAlerta.DescuadreCaja,
                    faltante ? SeveridadAlerta.Critica : SeveridadAlerta.Advertencia,
                    faltante ? "Faltante en caja" : "Sobrante en caja",
                    $"Cierre de {turno.Usuario?.Nombre}: esperado {Pesos(turno.MontoEsperado ?? 0)}, " +
                    $"contado {Pesos(turno.MontoContado ?? 0)}. {(faltante ? "Faltan" : "Sobran")} {Pesos(Math.Abs(diferencia))}.",
                    ct);
            }

            await _uow.GuardarCambiosAsync(ct);
        });

    public async Task<int> RevisarTodoAsync(CancellationToken ct = default)
    {
        _creadas = 0;

        await EvaluarProductosInternoAsync(await _productos.ListarIdsAsync(ct), ct);
        await EvaluarClientesInternoAsync(null, ct);
        await EvaluarTurnosAbiertosAsync(ct);
        await _uow.GuardarCambiosAsync(ct);

        await GenerarResumenDiarioSiCorrespondeAsync(ct);
        await _uow.GuardarCambiosAsync(ct);

        return _creadas;
    }

    public async Task<List<AlertaDto>> ListarAsync(bool soloNoLeidas, bool soloActivas, CancellationToken ct = default) =>
        (await _alertas.ListarAsync(soloNoLeidas, soloActivas, 200, ct))
        .Select(a => new AlertaDto(a.Id, a.Tipo.ToString(), a.Severidad.ToString(), a.Titulo, a.Mensaje, a.Fecha, a.Leida, a.Activa))
        .ToList();

    public async Task<ResumenAlertasDto> ResumenAsync(CancellationToken ct = default)
    {
        var conteo = await _alertas.ContarNoLeidasAsync(ct);
        return new ResumenAlertasDto(
            conteo.Values.Sum(),
            conteo.GetValueOrDefault(SeveridadAlerta.Critica),
            conteo.GetValueOrDefault(SeveridadAlerta.Advertencia),
            conteo.GetValueOrDefault(SeveridadAlerta.Info));
    }

    public async Task MarcarLeidaAsync(int id, CancellationToken ct = default)
    {
        var alerta = await _alertas.ObtenerPorIdAsync(id, ct) ?? throw new NoEncontradoException("Alerta no encontrada.");
        alerta.MarcarLeida();
        await _uow.GuardarCambiosAsync(ct);
    }

    public Task<int> MarcarTodasLeidasAsync(CancellationToken ct = default) => _alertas.MarcarTodasLeidasAsync(ct);

    private async Task EvaluarProductosInternoAsync(List<int> ids, CancellationToken ct)
    {
        if (ids.Count == 0)
            return;

        foreach (var p in await _productos.ObtenerPorIdsAsync(ids, ct))
        {
            var claveBajo = $"stock-bajo:{p.Id}";
            var claveAgotado = $"agotado:{p.Id}";
            var claveMargen = $"margen:{p.Id}";

            if (!p.Activo)
            {
                await ResolverAsync(claveBajo, ct);
                await ResolverAsync(claveAgotado, ct);
                await ResolverAsync(claveMargen, ct);
                continue;
            }

            if (p.StockActual <= 0)
                await AsegurarActivaAsync(claveAgotado, TipoAlerta.ProductoAgotado, SeveridadAlerta.Critica,
                    "Producto agotado", $"{p.Nombre} se agotó. Hay que pedirlo al proveedor.", ct);
            else
                await ResolverAsync(claveAgotado, ct);

            if (p.StockActual > 0 && p.RequiereReabastecimiento)
                await AsegurarActivaAsync(claveBajo, TipoAlerta.StockBajo, SeveridadAlerta.Advertencia,
                    "Stock bajo", $"{p.Nombre}: quedan {p.StockActual} unidades (mínimo {p.StockMinimo}).", ct);
            else if (!p.RequiereReabastecimiento)
                await ResolverAsync(claveBajo, ct);

            var margen = p.PrecioVenta == 0 ? 0 : (p.PrecioVenta - p.PrecioCompra) / p.PrecioVenta * 100;
            if (margen < _opciones.MargenMinimoPorcentaje)
                await AsegurarActivaAsync(claveMargen, TipoAlerta.MargenBajo, SeveridadAlerta.Advertencia,
                    "Margen de ganancia bajo",
                    $"{p.Nombre}: compra {Pesos(p.PrecioCompra)}, venta {Pesos(p.PrecioVenta)} " +
                    $"(margen {margen.ToString("0.#", CultureInfo.InvariantCulture)}%, mínimo {_opciones.MargenMinimoPorcentaje}%). Revise el precio.",
                    ct);
            else
                await ResolverAsync(claveMargen, ct);
        }
    }

    private async Task EvaluarClientesInternoAsync(List<int>? ids, CancellationToken ct)
    {
        var clientes = ids is null
            ? await _clientes.ListarAsync(null, ct)
            : await _clientes.ObtenerPorIdsAsync(ids, ct);

        var deudores = (await _clientes.ListarDeudoresAsync(ids, ct))
            .ToDictionary(d => d.Cliente.Id, d => d.ReferenciaUtc);
        var limiteDias = DateTime.UtcNow.AddDays(-_opciones.DiasSinAbonar);

        foreach (var c in clientes)
        {
            var claveLimite = $"credito-limite:{c.Id}";
            var claveCartera = $"cartera:{c.Id}";

            var umbral = c.LimiteCredito * _opciones.PorcentajeLimiteCredito / 100m;
            if (c.LimiteCredito > 0 && c.SaldoActual >= umbral)
                await AsegurarActivaAsync(claveLimite, TipoAlerta.CreditoCercaDelLimite, SeveridadAlerta.Advertencia,
                    "Cliente cerca de su límite de crédito",
                    $"{c.Nombre} debe {Pesos(c.SaldoActual)} de un límite de {Pesos(c.LimiteCredito)}.", ct);
            else
                await ResolverAsync(claveLimite, ct);

            if (deudores.TryGetValue(c.Id, out var referencia) && referencia is not null && referencia < limiteDias)
            {
                var dias = (int)(DateTime.UtcNow - referencia.Value).TotalDays;
                await AsegurarActivaAsync(claveCartera, TipoAlerta.CarteraSinAbono, SeveridadAlerta.Advertencia,
                    "Fiado sin abonos",
                    $"{c.Nombre} debe {Pesos(c.SaldoActual)} y lleva {dias} días sin abonar" +
                    (string.IsNullOrWhiteSpace(c.Telefono) ? "." : $". Tel. {c.Telefono}."),
                    ct);
            }
            else
            {
                await ResolverAsync(claveCartera, ct);
            }
        }
    }

    private async Task EvaluarTurnosAbiertosAsync(CancellationToken ct)
    {
        var abiertos = await _caja.ListarTurnosAbiertosAsync(ct);
        var limite = DateTime.UtcNow.AddHours(-_opciones.HorasTurnoAbierto);

        foreach (var t in abiertos.Where(t => t.FechaApertura < limite))
        {
            var horas = (int)(DateTime.UtcNow - t.FechaApertura).TotalHours;
            await AsegurarActivaAsync($"turno-abierto:{t.Id}", TipoAlerta.TurnoAbiertoProlongado, SeveridadAlerta.Advertencia,
                "Caja sin cerrar",
                $"La caja de {t.Usuario?.Nombre} lleva {horas} horas abierta. ¿Se olvidó hacer el cierre?", ct);
        }

        var idsAbiertos = abiertos.Select(t => $"turno-abierto:{t.Id}").ToHashSet();
        foreach (var alerta in await _alertas.ListarActivasAsync(TipoAlerta.TurnoAbiertoProlongado, ct))
            if (!idsAbiertos.Contains(alerta.Clave))
                alerta.Resolver();
    }

    private async Task GenerarResumenDiarioSiCorrespondeAsync(CancellationToken ct)
    {
        if (!_opciones.ResumenDiarioHabilitado
            || !TimeOnly.TryParse(_opciones.ResumenDiarioHora, CultureInfo.InvariantCulture, out var hora)
            || TimeOnly.FromDateTime(DateTime.Now) < hora)
            return;

        var hoy = RangoFechas.HoyLocal();
        var clave = $"resumen:{hoy:yyyy-MM-dd}";
        if (await _alertas.ExisteClaveAsync(clave, ct))
            return;

        var d = await _dashboard.ObtenerAsync(hoy, ct);
        var mensaje =
            $"Ventas: {Pesos(d.VentasDelDia)} en {d.NumeroVentasDelDia} ventas " +
            $"(contado {Pesos(d.VentasContado)}, fiado {Pesos(d.VentasCredito)}).\n" +
            $"Ganancia bruta: {Pesos(d.UtilidadBrutaDelDia)}.\n" +
            $"Abonos recibidos: {Pesos(d.AbonosCartera)}. Gastos: {Pesos(d.EgresosDelDia)}.\n" +
            $"Cartera total: {Pesos(d.CarteraTotal)}. Inventario: {Pesos(d.ValorInventario)}.\n" +
            (d.AlertasStock.Count == 0
                ? "Inventario sin productos bajo el mínimo."
                : $"Por pedir ({d.AlertasStock.Count}): {string.Join(", ", d.AlertasStock.Take(10).Select(a => $"{a.Nombre} ({a.StockActual})"))}.");

        await RegistrarEventoAsync(clave, TipoAlerta.ResumenDiario, SeveridadAlerta.Info,
            $"Resumen del {hoy:dd/MM/yyyy}", mensaje, ct);
    }

    private async Task AsegurarActivaAsync(string clave, TipoAlerta tipo, SeveridadAlerta severidad,
        string titulo, string mensaje, CancellationToken ct)
    {
        if (await _alertas.ObtenerActivaPorClaveAsync(clave, ct) is not null)
            return;

        await CrearAsync(new Alerta { Clave = clave, Tipo = tipo, Severidad = severidad, Titulo = titulo, Mensaje = mensaje }, ct);
    }

    private async Task RegistrarEventoAsync(string clave, TipoAlerta tipo, SeveridadAlerta severidad,
        string titulo, string mensaje, CancellationToken ct)
    {
        if (await _alertas.ExisteClaveAsync(clave, ct))
            return;

        await CrearAsync(new Alerta
        {
            Clave = clave, Tipo = tipo, Severidad = severidad, Titulo = titulo, Mensaje = mensaje, Activa = false
        }, ct);
    }

    private async Task CrearAsync(Alerta alerta, CancellationToken ct)
    {
        _alertas.Agregar(alerta);
        _creadas++;

        var notificar = _whatsApp.Habilitado
                        && (alerta.Severidad >= _opciones.SeveridadMinimaWhatsApp || alerta.Tipo == TipoAlerta.ResumenDiario);
        if (!notificar)
            return;

        var icono = alerta.Severidad switch
        {
            SeveridadAlerta.Critica => "🔴",
            SeveridadAlerta.Advertencia => "⚠️",
            _ => "📊"
        };
        var texto = $"{Marca} {icono} {alerta.Titulo}\n{alerta.Mensaje}";

        foreach (var destinatario in await _alertas.ListarDestinatariosAsync(soloActivos: true, ct))
            _alertas.AgregarNotificacion(new NotificacionWhatsApp
            {
                Alerta = alerta,
                DestinatarioId = destinatario.Id,
                Mensaje = texto
            });
    }

    private async Task ResolverAsync(string clave, CancellationToken ct)
    {
        var alerta = await _alertas.ObtenerActivaPorClaveAsync(clave, ct);
        alerta?.Resolver();
    }

    private async Task SinRomperAsync(string contexto, Func<Task> accion)
    {
        try
        {
            await accion();
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            _logger.LogError(e, "No se pudieron evaluar las alertas de {Contexto}", contexto);
        }
    }

    private static string Pesos(decimal valor) =>
        "$" + valor.ToString("N0", new NumberFormatInfo { NumberGroupSeparator = ".", NumberDecimalDigits = 0 });
}
