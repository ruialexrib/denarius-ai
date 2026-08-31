using DenariusAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DenariusAI.Infrastructure.Persistence.Configurations;

/// <summary>Configures persistence for dated stock prices.</summary>
internal sealed class StockPriceConfiguration : IEntityTypeConfiguration<StockPrice>
{
    /// <summary>Configures the stock price entity.</summary>
    /// <param name="builder">The entity type builder.</param>
    public void Configure(EntityTypeBuilder<StockPrice> builder)
    {
        builder.ToTable("StockPrices"); builder.HasKey(x => x.Id); builder.Property(x => x.Date).HasColumnType("date"); builder.Property(x => x.Price).HasPrecision(19, 6);
        builder.HasIndex(x => new { x.StockPositionId, x.Date }).IsUnique();
        builder.HasOne(x => x.StockPosition).WithMany().HasForeignKey(x => x.StockPositionId).OnDelete(DeleteBehavior.Cascade);
    }
}
