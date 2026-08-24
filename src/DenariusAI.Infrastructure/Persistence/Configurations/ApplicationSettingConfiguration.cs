using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DenariusAI.Infrastructure.Persistence.Configurations;

internal sealed class ApplicationSettingConfiguration : IEntityTypeConfiguration<ApplicationSetting>
{
    public void Configure(EntityTypeBuilder<ApplicationSetting> builder)
    {
        builder.ToTable("ApplicationSettings");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Key).HasMaxLength(150).IsRequired();
        builder.Property(item => item.Value).HasColumnType("nvarchar(max)").IsRequired();
        builder.HasIndex(item => item.Key).IsUnique();
    }
}
