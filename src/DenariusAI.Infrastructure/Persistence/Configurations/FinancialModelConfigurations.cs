using DenariusAI.Domain.Common;
using DenariusAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DenariusAI.Infrastructure.Persistence.Configurations;

/// <summary>
/// Provides extension methods for Entity Framework Core entity type configuration.
/// </summary>
internal static class ConfigurationExtensions
{
    /// <summary>
    /// Configures common auditing properties for entities that inherit from <see cref="AuditableEntity"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity type that inherits from <see cref="AuditableEntity"/>.</typeparam>
    /// <param name="builder">The entity type builder.</param>
    public static void ConfigureAuditing<TEntity>(this EntityTypeBuilder<TEntity> builder) where TEntity : AuditableEntity
    {
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.CreatedAt).IsRequired();
        builder.Property(entity => entity.CreatedBy).HasMaxLength(450);
        builder.Property(entity => entity.UpdatedBy).HasMaxLength(450);
    }
}

/// <summary>
/// Entity Framework Core configuration for the <see cref="FinancialGroup"/> entity.
/// </summary>
internal sealed class FinancialGroupConfiguration : IEntityTypeConfiguration<FinancialGroup>
{
    /// <summary>
    /// Configures the <see cref="FinancialGroup"/> entity.
    /// </summary>
    /// <param name="builder">The entity type builder.</param>
    public void Configure(EntityTypeBuilder<FinancialGroup> builder)
    {
        builder.ToTable("FinancialGroups"); builder.ConfigureAuditing();
        builder.Property(entity => entity.Name).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.Description).HasMaxLength(500);
        builder.HasIndex(entity => entity.Name).IsUnique();
        builder.HasIndex(entity => new { entity.Kind, entity.SortOrder });
        builder.HasData(StructuralSeed.Groups);
    }
}

/// <summary>
/// Entity Framework Core configuration for the <see cref="Category"/> entity.
/// </summary>
internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    /// <summary>
    /// Configures the <see cref="Category"/> entity.
    /// </summary>
    /// <param name="builder">The entity type builder.</param>
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories"); builder.ConfigureAuditing();
        builder.Property(entity => entity.Name).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.Description).HasMaxLength(500);
        builder.HasIndex(entity => entity.FinancialGroupId);
        builder.HasIndex(entity => new { entity.FinancialGroupId, entity.Name }).IsUnique();
        builder.HasIndex(entity => new { entity.FinancialGroupId, entity.SortOrder });
        builder.HasOne(entity => entity.FinancialGroup).WithMany(group => group.Categories).HasForeignKey(entity => entity.FinancialGroupId).OnDelete(DeleteBehavior.Restrict);
        builder.HasData(StructuralSeed.Categories);
    }
}

/// <summary>
/// Entity Framework Core configuration for the <see cref="Account"/> entity.
/// </summary>
internal sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    /// <summary>
    /// Configures the <see cref="Account"/> entity.
    /// </summary>
    /// <param name="builder">The entity type builder.</param>
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("Accounts", table => table.HasCheckConstraint("CK_Accounts_Currency", "LEN([Currency]) = 3")); builder.ConfigureAuditing();
        builder.Property(entity => entity.Name).HasMaxLength(120).IsRequired();
        builder.Property(entity => entity.Description).HasMaxLength(500);
        builder.Property(entity => entity.InitialBalance).HasPrecision(19, 4);
        builder.Property(entity => entity.Currency).HasMaxLength(3).IsFixedLength().IsUnicode(false).IsRequired();
        builder.HasIndex(entity => entity.Name);
        builder.HasIndex(entity => entity.CategoryId);
        builder.HasOne(entity => entity.Category).WithMany(category => category.Accounts).HasForeignKey(entity => entity.CategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasData(StructuralSeed.Accounts);
    }
}

/// <summary>
/// Entity Framework Core configuration for the <see cref="JournalEntry"/> entity.
/// </summary>
internal sealed class JournalEntryConfiguration : IEntityTypeConfiguration<JournalEntry>
{
    /// <summary>
    /// Configures the <see cref="JournalEntry"/> entity.
    /// </summary>
    /// <param name="builder">The entity type builder.</param>
    public void Configure(EntityTypeBuilder<JournalEntry> builder)
    {
        builder.ToTable("JournalEntries"); builder.ConfigureAuditing();
        builder.Property(entity => entity.Date).HasColumnType("date");
        builder.Property(entity => entity.Description).HasMaxLength(250).IsRequired();
        builder.Property(entity => entity.Reference).HasMaxLength(100);
        builder.Property(entity => entity.Notes).HasMaxLength(2000);
        builder.Property(entity => entity.CancelledBy).HasMaxLength(450);
        builder.Ignore(entity => entity.TotalDebit); builder.Ignore(entity => entity.TotalCredit); builder.Ignore(entity => entity.Difference);
        builder.HasIndex(entity => entity.Date);
        builder.HasIndex(entity => new { entity.Status, entity.Date });
        builder.HasMany(entity => entity.Lines).WithOne(line => line.JournalEntry).HasForeignKey(line => line.JournalEntryId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(entity => entity.Budget).WithMany(budget => budget.JournalEntries).HasForeignKey(entity => entity.BudgetId).OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(entity => entity.BudgetId);
        builder.Navigation(entity => entity.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasData(StructuralSeed.JournalEntries);
    }
}

/// <summary>
/// Entity Framework Core configuration for the <see cref="JournalEntryLine"/> entity.
/// </summary>
internal sealed class JournalEntryLineConfiguration : IEntityTypeConfiguration<JournalEntryLine>
{
    /// <summary>
    /// Configures the <see cref="JournalEntryLine"/> entity.
    /// </summary>
    /// <param name="builder">The entity type builder.</param>
    public void Configure(EntityTypeBuilder<JournalEntryLine> builder)
    {
        builder.ToTable("JournalEntryLines", table => table.HasCheckConstraint("CK_JournalEntryLines_DebitCredit", "([Debit] > 0 AND [Credit] = 0) OR ([Credit] > 0 AND [Debit] = 0)")); builder.ConfigureAuditing();
        builder.Property(entity => entity.Debit).HasPrecision(19, 4);
        builder.Property(entity => entity.Credit).HasPrecision(19, 4);
        builder.Property(entity => entity.Description).HasMaxLength(250);
        builder.HasIndex(entity => entity.JournalEntryId);
        builder.HasIndex(entity => entity.AccountId);
        builder.HasIndex(entity => entity.CategoryId);
        builder.HasOne(entity => entity.Account).WithMany(account => account.JournalEntryLines).HasForeignKey(entity => entity.AccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(entity => entity.Category).WithMany(category => category.JournalEntryLines).HasForeignKey(entity => entity.CategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasData(StructuralSeed.JournalEntryLines);
    }
}

/// <summary>
/// Entity Framework Core configuration for the <see cref="Budget"/> entity.
/// </summary>
internal sealed class BudgetConfiguration : IEntityTypeConfiguration<Budget>
{
    /// <summary>
    /// Configures the <see cref="Budget"/> entity.
    /// </summary>
    /// <param name="builder">The entity type builder.</param>
    public void Configure(EntityTypeBuilder<Budget> builder)
    {
        builder.ToTable("Budgets", table => { table.HasCheckConstraint("CK_Budgets_Month", "[Month] BETWEEN 1 AND 12"); table.HasCheckConstraint("CK_Budgets_Year", "[Year] BETWEEN 2000 AND 9999"); }); builder.ConfigureAuditing();
        builder.HasIndex(entity => new { entity.Year, entity.Month }).IsUnique();
        builder.HasMany(entity => entity.Lines).WithOne(line => line.Budget).HasForeignKey(line => line.BudgetId).OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>
/// Entity Framework Core configuration for the <see cref="BudgetLine"/> entity.
/// </summary>
internal sealed class BudgetLineConfiguration : IEntityTypeConfiguration<BudgetLine>
{
    /// <summary>
    /// Configures the <see cref="BudgetLine"/> entity.
    /// </summary>
    /// <param name="builder">The entity type builder.</param>
    public void Configure(EntityTypeBuilder<BudgetLine> builder)
    {
        builder.ToTable("BudgetLines", table => table.HasCheckConstraint("CK_BudgetLines_Amount", "[Amount] >= 0")); builder.ConfigureAuditing();
        builder.Property(entity => entity.Amount).HasPrecision(19, 4);
        builder.HasIndex(entity => entity.CategoryId);
        builder.HasIndex(entity => new { entity.BudgetId, entity.CategoryId }).IsUnique();
        builder.HasOne(entity => entity.Category).WithMany(category => category.BudgetLines).HasForeignKey(entity => entity.CategoryId).OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>
/// Entity Framework Core configuration for the <see cref="Reconciliation"/> entity.
/// </summary>
internal sealed class ReconciliationConfiguration : IEntityTypeConfiguration<Reconciliation>
{
    /// <summary>
    /// Configures the <see cref="Reconciliation"/> entity.
    /// </summary>
    /// <param name="builder">The entity type builder.</param>
    public void Configure(EntityTypeBuilder<Reconciliation> builder)
    {
        builder.ToTable("Reconciliations"); builder.ConfigureAuditing();
        builder.Property(entity => entity.ReconciledBy).HasMaxLength(450);
        builder.HasIndex(entity => entity.JournalEntryId).IsUnique();
        builder.HasIndex(entity => entity.Status);
        builder.HasOne(entity => entity.JournalEntry).WithOne(entry => entry.Reconciliation).HasForeignKey<Reconciliation>(entity => entity.JournalEntryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasData(StructuralSeed.Reconciliations);
    }
}
