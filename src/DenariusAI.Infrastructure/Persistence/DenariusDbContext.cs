using DenariusAI.Infrastructure.Identity;
using DenariusAI.Domain.Entities;
using DenariusAI.Domain.Common;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DenariusAI.Infrastructure.Persistence;

public sealed class DenariusDbContext(DbContextOptions<DenariusDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<FinancialGroup> FinancialGroups => Set<FinancialGroup>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<JournalEntryLine> JournalEntryLines => Set<JournalEntryLine>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<BudgetLine> BudgetLines => Set<BudgetLine>();
    public DbSet<Reconciliation> Reconciliations => Set<Reconciliation>();
    public DbSet<ApplicationSetting> ApplicationSettings => Set<ApplicationSetting>();
    public DbSet<SavingsCertificate> SavingsCertificates => Set<SavingsCertificate>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema("denarius");
        builder.ApplyConfigurationsFromAssembly(typeof(DenariusDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditTimestamps();
        ValidateJournalEntries();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyAuditTimestamps()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var trackedEntry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (trackedEntry.State == EntityState.Added && trackedEntry.Entity.CreatedAt == default)
                trackedEntry.Entity.CreatedAt = now;
            if (trackedEntry.State == EntityState.Modified)
                trackedEntry.Entity.UpdatedAt = now;
        }
    }

    private void ValidateJournalEntries()
    {
        var entries = ChangeTracker.Entries<JournalEntry>()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified)
            .Select(entry => entry.Entity)
            .Concat(ChangeTracker.Entries<JournalEntryLine>()
                .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
                .Select(entry => entry.Entity.JournalEntry))
            .Where(entry => entry is not null)
            .Distinct();

        foreach (var entry in entries)
        {
            entry.EnsureBalanced();
        }
    }
}
