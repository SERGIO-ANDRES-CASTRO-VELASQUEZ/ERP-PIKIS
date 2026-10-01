using ErpPikis.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErpPikis.Repository.Configurations;

public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("clientes", t =>
        {
            t.HasCheckConstraint("ck_clientes_saldo", "\"SaldoActual\" >= 0 AND \"SaldoActual\" <= \"LimiteCredito\"");
        });
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Nombre).HasMaxLength(120).IsRequired();
        builder.Property(c => c.Telefono).HasMaxLength(20);
        builder.Property(c => c.LimiteCredito).HasPrecision(12, 2);
        builder.Property(c => c.SaldoActual).HasPrecision(12, 2);
        builder.Property<uint>("Version").IsRowVersion();
        builder.Ignore(c => c.CreditoDisponible);
    }
}

public class VentaConfiguration : IEntityTypeConfiguration<Venta>
{
    public void Configure(EntityTypeBuilder<Venta> builder)
    {
        builder.ToTable("ventas");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Total).HasPrecision(12, 2);
        builder.Property(v => v.MontoRecibido).HasPrecision(12, 2);
        builder.Ignore(v => v.Cambio);
        builder.Property(v => v.TipoPago).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(v => v.Fecha);

        builder.HasOne(v => v.Usuario).WithMany().HasForeignKey(v => v.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(v => v.Cliente).WithMany().HasForeignKey(v => v.ClienteId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(v => v.TurnoCaja).WithMany(t => t.Ventas).HasForeignKey(v => v.TurnoCajaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(v => v.Detalles).WithOne(d => d.Venta).HasForeignKey(d => d.VentaId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class DetalleVentaConfiguration : IEntityTypeConfiguration<DetalleVenta>
{
    public void Configure(EntityTypeBuilder<DetalleVenta> builder)
    {
        builder.ToTable("detalle_ventas", t =>
        {
            t.HasCheckConstraint("ck_detalle_ventas_cantidad", "\"Cantidad\" > 0");
        });
        builder.HasKey(d => d.Id);
        builder.Property(d => d.PrecioUnitario).HasPrecision(12, 2);
        builder.Property(d => d.CostoUnitario).HasPrecision(12, 2);
        builder.Property(d => d.Subtotal).HasPrecision(12, 2);
        builder.Ignore(d => d.Utilidad);

        builder.HasOne(d => d.Producto).WithMany().HasForeignKey(d => d.ProductoId).OnDelete(DeleteBehavior.Restrict);
    }
}
