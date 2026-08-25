using DenariusAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DenariusAI.Infrastructure.Persistence.Configurations;

internal sealed class ReminderConfiguration : IEntityTypeConfiguration<Reminder>
{
    public void Configure(EntityTypeBuilder<Reminder> builder)
    {
        builder.ToTable("Reminders"); builder.ConfigureAuditing();
        builder.Property(item => item.Text).HasMaxLength(500).IsRequired();
        builder.Property(item => item.EventDate).HasColumnType("date");
        builder.HasIndex(item => item.EventDate);
        builder.HasMany(item => item.Acknowledgements).WithOne(item => item.Reminder).HasForeignKey(item => item.ReminderId).OnDelete(DeleteBehavior.Cascade);
        builder.HasData(StructuralSeed.Reminders);
    }
}

internal sealed class ReminderAcknowledgementConfiguration : IEntityTypeConfiguration<ReminderAcknowledgement>
{
    public void Configure(EntityTypeBuilder<ReminderAcknowledgement> builder)
    {
        builder.ToTable("ReminderAcknowledgements");
        builder.HasKey(item => new { item.ReminderId, item.UserId });
        builder.Property(item => item.UserId).HasMaxLength(450);
        builder.HasIndex(item => item.UserId);
    }
}
