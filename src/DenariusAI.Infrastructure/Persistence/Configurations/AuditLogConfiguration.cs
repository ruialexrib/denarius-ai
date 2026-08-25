using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DenariusAI.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.EntityType).HasMaxLength(128).IsRequired();
        builder.Property(item => item.EntityId).HasMaxLength(450).IsRequired();
        builder.Property(item => item.RecordLabel).HasMaxLength(500);
        builder.Property(item => item.Action).HasMaxLength(20).IsRequired();
        builder.Property(item => item.UserId).HasMaxLength(450);
        builder.Property(item => item.UserName).HasMaxLength(256);
        builder.HasIndex(item => new { item.EntityType, item.EntityId, item.ChangedAt });
        builder.HasIndex(item => new { item.UserId, item.ChangedAt });
        builder.HasIndex(item => new { item.Action, item.ChangedAt });
    }
}
