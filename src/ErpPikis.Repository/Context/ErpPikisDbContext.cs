using ErpPikis.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ErpPikis.Repository.Context;

public class ErpPikisDbContext : DbContext
{
    public ErpPikisDbContext(DbContextOptions<ErpPikisDbContext> options) : base(options)
    {
    }

    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Venta> Ventas => Set<Venta>();
    public DbSet<DetalleVenta> DetallesVenta => Set<DetalleVenta>();
    public DbSet<TurnoCaja> TurnosCaja => Set<TurnoCaja>();
    public DbSet<MovimientoCaja> MovimientosCaja => Set<MovimientoCaja>();
    public DbSet<MovimientoInventario> MovimientosInventario => Set<MovimientoInventario>();
    public DbSet<Alerta> Alertas => Set<Alerta>();
    public DbSet<DestinatarioAlerta> DestinatariosAlerta => Set<DestinatarioAlerta>();
    public DbSet<NotificacionWhatsApp> NotificacionesWhatsApp => Set<NotificacionWhatsApp>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ErpPikisDbContext).Assembly);
    }
}
