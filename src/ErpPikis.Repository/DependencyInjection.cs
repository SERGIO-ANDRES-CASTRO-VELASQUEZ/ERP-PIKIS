using ErpPikis.Repository.Context;
using ErpPikis.Repository.Interfaces;
using ErpPikis.Repository.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ErpPikis.Repository;

public static class DependencyInjection
{
    public static IServiceCollection AddRepositorio(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<ErpPikisDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<ICategoriaRepository, CategoriaRepository>();
        services.AddScoped<IProductoRepository, ProductoRepository>();
        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<IVentaRepository, VentaRepository>();
        services.AddScoped<ICajaRepository, CajaRepository>();
        services.AddScoped<IAlertaRepository, AlertaRepository>();

        return services;
    }

    public static async Task AplicarMigracionesAsync(this IServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ErpPikisDbContext>();
        await db.Database.MigrateAsync();
    }
}
