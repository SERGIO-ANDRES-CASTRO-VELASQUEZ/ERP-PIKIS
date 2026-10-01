using ErpPikis.Service.Alertas;
using ErpPikis.Service.Impresion;
using ErpPikis.Service.Interfaces;
using ErpPikis.Service.Services;
using Microsoft.Extensions.DependencyInjection;
using QuestPDF.Infrastructure;

namespace ErpPikis.Service;

public static class DependencyInjection
{
    /// <summary>
    /// Quien llama debe registrar además un <see cref="IWhatsAppSender"/> (ver capa Api).
    /// </summary>
    public static IServiceCollection AddServicios(
        this IServiceCollection services,
        TiendaOptions tienda,
        ImpresoraOptions impresora,
        AlertasOptions alertas,
        EnvioWhatsAppOptions envioWhatsApp)
    {
        services.AddSingleton(alertas);
        services.AddSingleton(envioWhatsApp);
        services.AddScoped<IAlertaService, AlertaService>();
        services.AddScoped<INotificacionService, NotificacionService>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUsuarioService, UsuarioService>();
        services.AddScoped<ICategoriaService, CategoriaService>();
        services.AddScoped<IProductoService, ProductoService>();
        services.AddScoped<IClienteService, ClienteService>();
        services.AddScoped<IVentaService, VentaService>();
        services.AddScoped<ICajaService, CajaService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IDatosInicialesService, DatosInicialesService>();

        QuestPDF.Settings.License = LicenseType.Community;

        services.AddSingleton(tienda);
        services.AddSingleton(impresora);
        services.AddSingleton<IImpresoraTermica>(impresora.Modo switch
        {
            ModoImpresora.Red => new ImpresoraRed(impresora),
            ModoImpresora.Windows => new ImpresoraWindows(impresora),
            _ => new ImpresoraNoConfigurada()
        });
        services.AddScoped<ITicketService, TicketService>();

        return services;
    }
}
