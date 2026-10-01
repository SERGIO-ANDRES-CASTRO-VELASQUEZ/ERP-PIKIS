using ErpPikis.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErpPikis.Repository.Configurations;

public class TurnoCajaConfiguration : IEntityTypeConfiguration<TurnoCaja>
{
    public void Configure(EntityTypeBuilder<TurnoCaja> builder)
    {
        builder.ToTable("turnos_caja");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.MontoInicial).HasPrecision(12, 2);
        builder.Property(t => t.MontoEsperado).HasPrecision(12, 2);
        builder.Property(t => t.MontoContado).HasPrecision(12, 2);
        builder.Property(t => t.Diferencia).HasPrecision(12, 2);
        builder.Ignore(t => t.Abierto);

        builder.HasIndex(t => t.UsuarioId)
            .IsUnique()
            .HasFilter("\"FechaCierre\" IS NULL")
            .HasDatabaseName("ux_turnos_caja_un_abierto_por_usuario");

        builder.HasOne(t => t.Usuario).WithMany().HasForeignKey(t => t.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(t => t.Movimientos).WithOne(m => m.TurnoCaja).HasForeignKey(m => m.TurnoCajaId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class MovimientoCajaConfiguration : IEntityTypeConfiguration<MovimientoCaja>
{
    public void Configure(EntityTypeBuilder<MovimientoCaja> builder)
    {
        builder.ToTable("movimientos_caja", t =>
        {
            t.HasCheckConstraint("ck_movimientos_caja_monto", "\"Monto\" >= 0");
        });
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Tipo).HasConversion<string>().HasMaxLength(20);
        builder.Property(m => m.Monto).HasPrecision(12, 2);
        builder.Property(m => m.Descripcion).HasMaxLength(250).IsRequired();
        builder.HasIndex(m => m.Fecha);

        builder.HasOne(m => m.Usuario).WithMany().HasForeignKey(m => m.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(m => m.Cliente).WithMany().HasForeignKey(m => m.ClienteId).OnDelete(DeleteBehavior.Restrict);
    }
}
