using DenariusAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DenariusAI.Infrastructure.Persistence.Configurations;

internal sealed class SavingsCertificateConfiguration : IEntityTypeConfiguration<SavingsCertificate>
{
    public void Configure(EntityTypeBuilder<SavingsCertificate> builder)
    {
        builder.ToTable("SavingsCertificates");
        builder.ConfigureAuditing();
        builder.Property(item => item.InvestmentDate).HasColumnType("date");
        builder.Property(item => item.SeriesNumber).HasMaxLength(80).IsRequired();
        builder.Property(item => item.Description).HasMaxLength(200).IsRequired();
        builder.Property(item => item.InvestmentValue).HasPrecision(19, 4);
        builder.Property(item => item.Rate).HasPrecision(9, 6);
        builder.Property(item => item.CurrentValue).HasPrecision(19, 4);
        builder.Property(item => item.NextCapitalization).HasColumnType("date");
        builder.HasIndex(item => item.SeriesNumber).IsUnique();
        builder.HasIndex(item => item.InvestmentDate);
    }
}
