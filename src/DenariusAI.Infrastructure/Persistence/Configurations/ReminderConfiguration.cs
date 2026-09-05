using DenariusAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DenariusAI.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures the entity type mapping for the <see cref="Reminder"/> entity.
/// </summary>
internal sealed class ReminderConfiguration : IEntityTypeConfiguration<Reminder>
{
    /// <summary>
    /// Configures the <see cref="Reminder"/> entity type.
    /// </summary>
    /// <param name="builder">The builder to be used to configure the entity type.</param>
    public void Configure(EntityTypeBuilder<Reminder> builder)
    {
        builder.ToTable("Reminders"); builder.ConfigureAuditing();
        builder.Property(item => item.Text).HasMaxLength(500).IsRequired();
        builder.Property(item => item.EventDate).HasColumnType("date");
        builder.HasIndex(item => item.EventDate);
        builder.HasIndex(item => item.WarrantyId).IsUnique().HasFilter("[WarrantyId] IS NOT NULL");
        builder.HasIndex(item => item.SavingsCertificateId).IsUnique().HasFilter("[SavingsCertificateId] IS NOT NULL");
        builder.HasMany(item => item.Acknowledgements).WithOne(item => item.Reminder).HasForeignKey(item => item.ReminderId).OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// Configures the entity type mapping for the <see cref="ReminderAcknowledgement"/> entity.
/// </summary>
internal sealed class ReminderAcknowledgementConfiguration : IEntityTypeConfiguration<ReminderAcknowledgement>
{
    /// <summary>
    /// Configures the <see cref="ReminderAcknowledgement"/> entity type.
    /// </summary>
    /// <param name="builder">The builder to be used to configure the entity type.</param>
    public void Configure(EntityTypeBuilder<ReminderAcknowledgement> builder)
    {
        builder.ToTable("ReminderAcknowledgements");
        builder.HasKey(item => new { item.ReminderId, item.UserId });
        builder.Property(item => item.UserId).HasMaxLength(450);
        builder.HasIndex(item => item.UserId);
    }
}
