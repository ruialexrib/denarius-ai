using DenariusAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DenariusAI.Infrastructure.Persistence.Configurations;

internal sealed class WarrantyConfiguration : IEntityTypeConfiguration<Warranty>
{
    public void Configure(EntityTypeBuilder<Warranty> builder)
    {
        builder.ToTable("Warranties"); builder.ConfigureAuditing();
        builder.Property(item => item.Name).HasMaxLength(200).IsRequired();
        builder.Property(item => item.Supplier).HasMaxLength(200);
        builder.Property(item => item.PurchaseDate).HasColumnType("date");
        builder.Property(item => item.ExpiryDate).HasColumnType("date");
        builder.Property(item => item.Notes).HasMaxLength(2000);
        ConfigureDocument(builder);
        builder.HasIndex(item => item.ExpiryDate);
        builder.HasOne(item => item.Reminder).WithOne(item => item.Warranty).HasForeignKey<Reminder>(item => item.WarrantyId).OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureDocument(EntityTypeBuilder<Warranty> builder)
    {
        builder.Property(item => item.DocumentFileName).HasMaxLength(255);
        builder.Property(item => item.DocumentContentType).HasMaxLength(100).IsRequired();
        builder.Property(item => item.DocumentBase64).HasColumnType("nvarchar(max)");
    }
}

internal sealed class CorrespondenceConfiguration : IEntityTypeConfiguration<Correspondence>
{
    public void Configure(EntityTypeBuilder<Correspondence> builder)
    {
        builder.ToTable("Correspondence"); builder.ConfigureAuditing();
        builder.Property(item => item.Subject).HasMaxLength(250).IsRequired();
        builder.Property(item => item.Sender).HasMaxLength(200);
        builder.Property(item => item.ReceivedDate).HasColumnType("date");
        builder.Property(item => item.Notes).HasMaxLength(2000);
        builder.Property(item => item.DocumentFileName).HasMaxLength(255);
        builder.Property(item => item.DocumentContentType).HasMaxLength(100).IsRequired();
        builder.Property(item => item.DocumentBase64).HasColumnType("nvarchar(max)");
        builder.HasIndex(item => item.ReceivedDate);
        builder.HasMany(item => item.Metadata).WithOne(item => item.Correspondence).HasForeignKey(item => item.CorrespondenceId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class CorrespondenceMetadataConfiguration : IEntityTypeConfiguration<CorrespondenceMetadata>
{
    public void Configure(EntityTypeBuilder<CorrespondenceMetadata> builder)
    {
        builder.ToTable("CorrespondenceMetadata"); builder.ConfigureAuditing();
        builder.Property(item => item.Key).HasMaxLength(120).IsRequired();
        builder.Property(item => item.Value).HasMaxLength(1000).IsRequired();
        builder.Property(item => item.Confidence).HasMaxLength(10).IsUnicode(false);
        builder.HasIndex(item => item.CorrespondenceId);
        builder.HasIndex(item => new { item.CorrespondenceId, item.Key }).IsUnique();
    }
}
