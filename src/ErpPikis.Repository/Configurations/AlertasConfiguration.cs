using ErpPikis.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErpPikis.Repository.Configurations;

public class AlertaConfiguration : IEntityTypeConfiguration<Alerta>
{
    public void Configure(EntityTypeBuilder<Alerta> builder)
    {
        builder.ToTable("alertas");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Tipo).HasConversion<string>().HasMaxLength(40);
        builder.Property(a => a.Severidad).HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.Clave).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Titulo).HasMaxLength(150).IsRequired();
        builder.Property(a => a.Mensaje).HasMaxLength(2000).IsRequired();

        builder.HasIndex(a => a.Clave, "ux_alertas_clave_activa")
            .IsUnique()
            .HasFilter("\"Activa\"");
        builder.HasIndex(a => a.Clave, "ix_alertas_clave");
        builder.HasIndex(a => new { a.Leida, a.Fecha });
    }
}

public class DestinatarioAlertaConfiguration : IEntityTypeConfiguration<DestinatarioAlerta>
{
    public void Configure(EntityTypeBuilder<DestinatarioAlerta> builder)
    {
        builder.ToTable("destinatarios_alerta");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Nombre).HasMaxLength(120).IsRequired();
        builder.Property(d => d.Telefono).HasMaxLength(20).IsRequired();
        builder.Property(d => d.ApiKey).HasMaxLength(100);
        builder.HasIndex(d => d.Telefono).IsUnique();
    }
}

public class NotificacionWhatsAppConfiguration : IEntityTypeConfiguration<NotificacionWhatsApp>
{
    public void Configure(EntityTypeBuilder<NotificacionWhatsApp> builder)
    {
        builder.ToTable("notificaciones_whatsapp");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Mensaje).HasMaxLength(2000).IsRequired();
        builder.Property(n => n.Estado).HasConversion<string>().HasMaxLength(20);
        builder.Property(n => n.UltimoError).HasMaxLength(500);
        builder.HasIndex(n => new { n.Estado, n.ProximoIntento });

        builder.HasOne(n => n.Alerta).WithMany().HasForeignKey(n => n.AlertaId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(n => n.Destinatario).WithMany().HasForeignKey(n => n.DestinatarioId).OnDelete(DeleteBehavior.Cascade);
    }
}
