using ErpPikis.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErpPikis.Repository.Configurations;

public class CategoriaConfiguration : IEntityTypeConfiguration<Categoria>
{
    public void Configure(EntityTypeBuilder<Categoria> builder)
    {
        builder.ToTable("categorias");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Nombre).HasMaxLength(80).IsRequired();
        builder.HasIndex(c => c.Nombre).IsUnique();

        builder.HasData(
            new Categoria { Id = 1, Nombre = "Víveres" },
            new Categoria { Id = 2, Nombre = "Abarrotes" },
            new Categoria { Id = 3, Nombre = "Golosinas" },
            new Categoria { Id = 4, Nombre = "Aseo" });
    }
}

public class ProductoConfiguration : IEntityTypeConfiguration<Producto>
{
    public void Configure(EntityTypeBuilder<Producto> builder)
    {
        builder.ToTable("productos", t =>
        {
            t.HasCheckConstraint("ck_productos_precio_venta", "\"PrecioVenta\" >= \"PrecioCompra\"");
            t.HasCheckConstraint("ck_productos_stock", "\"StockActual\" >= 0 AND \"StockMinimo\" >= 0");
        });
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Nombre).HasMaxLength(150).IsRequired();
        builder.Property(p => p.CodigoBarras).HasMaxLength(50);
        builder.Property(p => p.PrecioCompra).HasPrecision(12, 2);
        builder.Property(p => p.PrecioVenta).HasPrecision(12, 2);
        builder.HasIndex(p => p.CodigoBarras).IsUnique();
        builder.Property<uint>("Version").IsRowVersion();
        builder.Ignore(p => p.RequiereReabastecimiento);

        builder.HasOne(p => p.Categoria)
            .WithMany(c => c.Productos)
            .HasForeignKey(p => p.CategoriaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class MovimientoInventarioConfiguration : IEntityTypeConfiguration<MovimientoInventario>
{
    public void Configure(EntityTypeBuilder<MovimientoInventario> builder)
    {
        builder.ToTable("movimientos_inventario");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Tipo).HasConversion<string>().HasMaxLength(20);
        builder.Property(m => m.CostoUnitario).HasPrecision(12, 2);
        builder.Property(m => m.Motivo).HasMaxLength(250);
        builder.HasIndex(m => new { m.ProductoId, m.Fecha });

        builder.HasOne(m => m.Producto).WithMany().HasForeignKey(m => m.ProductoId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(m => m.Usuario).WithMany().HasForeignKey(m => m.UsuarioId).OnDelete(DeleteBehavior.Restrict);
    }
}
