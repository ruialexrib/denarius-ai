using DenariusAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DenariusAI.Infrastructure.Persistence.Configurations;

/// <summary>Configures persistence for stock positions.</summary>
internal sealed class StockPositionConfiguration : IEntityTypeConfiguration<StockPosition>
{
    /// <summary>Configures the stock position entity.</summary>
    /// <param name="builder">The entity type builder.</param>
    public void Configure(EntityTypeBuilder<StockPosition> builder)
    {
        builder.ToTable("StockPositions"); builder.ConfigureAuditing();
        builder.Property(x => x.Ticker).HasMaxLength(24).IsRequired(); builder.Property(x => x.Name).HasMaxLength(160).IsRequired(); builder.Property(x => x.Exchange).HasMaxLength(40); builder.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        builder.Property(x => x.Quantity).HasPrecision(19, 8); builder.Property(x => x.AverageCost).HasPrecision(19, 6); builder.Property(x => x.CurrentPrice).HasPrecision(19, 6); builder.Property(x => x.PriceDate).HasColumnType("date");
        builder.Property(x => x.HistoryStartDate).HasColumnType("date"); builder.Property(x => x.ForecastEnabled).HasDefaultValue(false); builder.Property(x => x.WatchlistOnly).HasDefaultValue(false);
        builder.HasIndex(x => new { x.Ticker, x.Exchange }).IsUnique();
    }
}
