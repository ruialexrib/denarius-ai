using DenariusAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DenariusAI.Infrastructure.Persistence.Configurations;

/// <summary>Configures persistence for insurance policies.</summary>
internal sealed class InsurancePolicyConfiguration : IEntityTypeConfiguration<InsurancePolicy>
{
    /// <summary>Configures the insurance policy table and relationships.</summary>
    /// <param name="builder">Entity type builder.</param>
    public void Configure(EntityTypeBuilder<InsurancePolicy> builder)
    {
        builder.ToTable("InsurancePolicies");
        builder.ConfigureAuditing();
        builder.Property(x => x.Name).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Insurer).HasMaxLength(160).IsRequired();
        builder.Property(x => x.PolicyNumber).HasMaxLength(100).IsRequired();
        builder.Property(x => x.InsuredSubject).HasMaxLength(240);
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.Property(x => x.StartDate).HasColumnType("date");
        builder.Property(x => x.EndDate).HasColumnType("date");
        builder.Property(x => x.RenewalDate).HasColumnType("date");
        builder.HasIndex(x => x.PolicyNumber);
        builder.HasIndex(x => x.RenewalDate);
        builder.HasMany(x => x.Premiums).WithOne(x => x.Policy).HasForeignKey(x => x.PolicyId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Attachments).WithOne(x => x.Policy).HasForeignKey(x => x.PolicyId).OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>Configures persistence for general insurance policy attachments.</summary>
internal sealed class InsurancePolicyAttachmentConfiguration : IEntityTypeConfiguration<InsurancePolicyAttachment>
{
    /// <summary>Configures the insurance policy attachment table.</summary>
    /// <param name="builder">Entity type builder.</param>
    public void Configure(EntityTypeBuilder<InsurancePolicyAttachment> builder)
    {
        builder.ToTable("InsurancePolicyAttachments");
        builder.ConfigureAuditing();
        builder.Property(x => x.FileName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.DocumentBase64).IsRequired();
    }
}

/// <summary>Configures persistence for insurance premiums.</summary>
internal sealed class InsurancePremiumConfiguration : IEntityTypeConfiguration<InsurancePremium>
{
    /// <summary>Configures the insurance premium table and relationships.</summary>
    /// <param name="builder">Entity type builder.</param>
    public void Configure(EntityTypeBuilder<InsurancePremium> builder)
    {
        builder.ToTable("InsurancePremiums");
        builder.ConfigureAuditing();
        builder.Property(x => x.Amount).HasPrecision(19, 4);
        builder.Property(x => x.PeriodStart).HasColumnType("date");
        builder.Property(x => x.PeriodEnd).HasColumnType("date");
        builder.Property(x => x.DueDate).HasColumnType("date");
        builder.Property(x => x.Reference).HasMaxLength(160);
        builder.Ignore(x => x.IsPaid);
        builder.Ignore(x => x.PaymentDate);
        builder.HasIndex(x => x.DueDate);
        builder.HasIndex(x => x.JournalEntryId);
        builder.HasOne(x => x.JournalEntry).WithMany().HasForeignKey(x => x.JournalEntryId).OnDelete(DeleteBehavior.SetNull);
        builder.HasMany(x => x.Attachments).WithOne(x => x.Premium).HasForeignKey(x => x.PremiumId).OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>Configures persistence for insurance premium attachments.</summary>
internal sealed class InsurancePremiumAttachmentConfiguration : IEntityTypeConfiguration<InsurancePremiumAttachment>
{
    /// <summary>Configures the insurance premium attachment table.</summary>
    /// <param name="builder">Entity type builder.</param>
    public void Configure(EntityTypeBuilder<InsurancePremiumAttachment> builder)
    {
        builder.ToTable("InsurancePremiumAttachments");
        builder.ConfigureAuditing();
        builder.Property(x => x.FileName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.DocumentBase64).IsRequired();
    }
}
