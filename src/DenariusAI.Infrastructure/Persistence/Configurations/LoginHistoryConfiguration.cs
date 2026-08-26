using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DenariusAI.Infrastructure.Persistence.Configurations;

public sealed class LoginHistoryConfiguration : IEntityTypeConfiguration<LoginHistory>
{
    public void Configure(EntityTypeBuilder<LoginHistory> builder)
    {
        builder.ToTable("LoginHistory");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.UserId).HasMaxLength(450).IsRequired();
        builder.Property(item => item.IpAddress).HasMaxLength(64).IsRequired();
        builder.HasIndex(item => new { item.UserId, item.LoggedInAt });
        builder.HasOne(item => item.User).WithMany().HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
