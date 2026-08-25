using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DenariusAI.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity type configuration for the ApplicationSetting entity.
/// Defines the database schema, constraints, and indexes.
/// </summary>
internal sealed class ApplicationSettingConfiguration : IEntityTypeConfiguration<ApplicationSetting>
{
    /// <summary>
    /// Configures the ApplicationSetting entity type.
    /// </summary>
    /// <param name="builder">The builder used to configure the entity type.</param>
    public void Configure(EntityTypeBuilder<ApplicationSetting> builder)
    {
        builder.ToTable("ApplicationSettings");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Key).HasMaxLength(150).IsRequired();
        builder.Property(item => item.Value).HasColumnType("nvarchar(max)").IsRequired();
        builder.HasIndex(item => item.Key).IsUnique();
    }
}
