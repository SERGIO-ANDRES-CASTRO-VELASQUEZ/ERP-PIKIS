using ErpPikis.Api.WhatsApp;
using ErpPikis.Service.Alertas;

namespace ErpPikis.Api.Workers;

/// <summary>Revisa periódicamente inventario, cartera y caja, y genera el resumen diario.</summary>
public class RevisionAlertasWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly AlertasOptions _opciones;
    private readonly ILogger<RevisionAlertasWorker> _logger;

    public RevisionAlertasWorker(IServiceScopeFactory scopes, AlertasOptions opciones, ILogger<RevisionAlertasWorker> logger)
    {
        _scopes = scopes;
        _opciones = opciones;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(Math.Max(1, _opciones.IntervaloRevisionMinutos)));

        do
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var creadas = await scope.ServiceProvider.GetRequiredService<IAlertaService>().RevisarTodoAsync(stoppingToken);
                if (creadas > 0)
                    _logger.LogInformation("Revisión de alertas: {Creadas} alertas nuevas", creadas);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                _logger.LogError(e, "Falló la revisión periódica de alertas");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}

/// <summary>Envía la cola de WhatsApp. Si no hay internet, los mensajes esperan y se reintentan.</summary>
public class EnvioWhatsAppWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly WhatsAppOptions _opciones;
    private readonly ILogger<EnvioWhatsAppWorker> _logger;

    public EnvioWhatsAppWorker(IServiceScopeFactory scopes, WhatsAppOptions opciones, ILogger<EnvioWhatsAppWorker> logger)
    {
        _scopes = scopes;
        _opciones = opciones;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_opciones.Proveedor == ProveedorWhatsApp.Ninguno)
            return;

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(5, _opciones.IntervaloEnvioSegundos)));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var enviadas = await scope.ServiceProvider.GetRequiredService<INotificacionService>().ProcesarPendientesAsync(stoppingToken);
                if (enviadas > 0)
                    _logger.LogInformation("WhatsApp: {Enviadas} mensajes enviados", enviadas);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                _logger.LogError(e, "Falló el envío de la cola de WhatsApp");
            }
        }
    }
}
