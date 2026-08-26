using DenariusAI.Infrastructure.Identity;
using DenariusAI.Domain.Entities;
using DenariusAI.Domain.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace DenariusAI.Infrastructure.Persistence;

/// <summary>
/// Database context for the Denarius AI application.
/// Manages entity configurations and audit operations for financial data.
/// </summary>
public sealed class DenariusDbContext(DbContextOptions<DenariusDbContext> options, IHttpContextAccessor? httpContextAccessor = null)
    : IdentityDbContext<ApplicationUser>(options)
{
    /// <summary>
    /// Gets or sets the collection of financial groups.
    /// </summary>
    public DbSet<FinancialGroup> FinancialGroups => Set<FinancialGroup>();
    
    /// <summary>
    /// Gets or sets the collection of categories.
    /// </summary>
    public DbSet<Category> Categories => Set<Category>();
    
    /// <summary>
    /// Gets or sets the collection of accounts.
    /// </summary>
    public DbSet<Account> Accounts => Set<Account>();
    
    /// <summary>
    /// Gets or sets the collection of journal entries.
    /// </summary>
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    
    /// <summary>
    /// Gets or sets the collection of journal entry lines.
    /// </summary>
    public DbSet<JournalEntryLine> JournalEntryLines => Set<JournalEntryLine>();
    
    /// <summary>
    /// Gets or sets the collection of budgets.
    /// </summary>
    public DbSet<Budget> Budgets => Set<Budget>();
    
    /// <summary>
    /// Gets or sets the collection of budget lines.
    /// </summary>
    public DbSet<BudgetLine> BudgetLines => Set<BudgetLine>();
    
    /// <summary>
    /// Gets or sets the collection of reconciliations.
    /// </summary>
    public DbSet<Reconciliation> Reconciliations => Set<Reconciliation>();
    
    /// <summary>
    /// Gets or sets the collection of application settings.
    /// </summary>
    public DbSet<ApplicationSetting> ApplicationSettings => Set<ApplicationSetting>();
    
    /// <summary>
    /// Gets or sets the collection of savings certificates.
    /// </summary>
    public DbSet<SavingsCertificate> SavingsCertificates => Set<SavingsCertificate>();
    
    /// <summary>
    /// Gets or sets the collection of reminders.
    /// </summary>
    public DbSet<Reminder> Reminders => Set<Reminder>();
    
    /// <summary>
    /// Gets or sets the collection of reminder acknowledgements.
    /// </summary>
    public DbSet<ReminderAcknowledgement> ReminderAcknowledgements => Set<ReminderAcknowledgement>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<LoginHistory> LoginHistory => Set<LoginHistory>();

    /// <summary>
    /// Configures the model and entity relationships.
    /// </summary>
    /// <param name="builder">The model builder used to configure entities.</param>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema("denarius");
        builder.ApplyConfigurationsFromAssembly(typeof(DenariusDbContext).Assembly);
    }

    /// <summary>
    /// Saves all changes made in this context to the database asynchronously.
    /// Applies audit timestamps and validates journal entries before saving.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous save operation. The task result contains the number of state entries written to the database.</returns>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditTimestamps();
        ValidateJournalEntries();
        CaptureAuditLogs();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void CaptureAuditLogs()
    {
        var entries = ChangeTracker.Entries()
            .Where(entry => entry.Entity is not AuditLog &&
                entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted &&
                (entry.Entity is AuditableEntity || entry.Entity is ApplicationUser or ReminderAcknowledgement))
            .ToList();
        if (entries.Count == 0) return;

        var principal = httpContextAccessor?.HttpContext?.User;
        var userId = principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        var userName = principal?.Identity?.Name;
        var now = DateTimeOffset.UtcNow;
        foreach (var entry in entries)
        {
            var key = string.Join("|", entry.Properties.Where(property => property.Metadata.IsPrimaryKey())
                .Select(property => (entry.State == EntityState.Deleted ? property.OriginalValue : property.CurrentValue)?.ToString()));
            if (string.IsNullOrWhiteSpace(key)) continue;

            var oldValues = new SortedDictionary<string, object?>();
            var newValues = new SortedDictionary<string, object?>();
            var changed = new List<string>();
            foreach (var property in entry.Properties)
            {
                if (IsSensitive(property.Metadata.Name))
                {
                    if (entry.State == EntityState.Modified && property.IsModified && !Equals(property.OriginalValue, property.CurrentValue))
                        changed.Add(property.Metadata.Name);
                    continue;
                }
                if (entry.State is EntityState.Modified or EntityState.Deleted)
                    oldValues[property.Metadata.Name] = property.OriginalValue;
                if (entry.State is EntityState.Added or EntityState.Modified)
                    newValues[property.Metadata.Name] = property.CurrentValue;
                if (entry.State == EntityState.Modified && property.IsModified && !Equals(property.OriginalValue, property.CurrentValue))
                    changed.Add(property.Metadata.Name);
            }
            if (entry.State == EntityState.Modified && changed.Count == 0) continue;

            var action = entry.State switch { EntityState.Added => "Created", EntityState.Modified => "Updated", _ => "Deleted" };
            var valuesForLabel = entry.State == EntityState.Deleted ? oldValues : newValues;
            AuditLogs.Add(new AuditLog
            {
                EntityType = entry.Metadata.ClrType.Name,
                EntityId = key,
                RecordLabel = FindLabel(valuesForLabel, key),
                Action = action,
                ChangedAt = now,
                UserId = userId ?? FindActor(entry, action),
                UserName = userName,
                ChangedColumns = changed.Count == 0 ? null : JsonSerializer.Serialize(changed),
                OldValues = oldValues.Count == 0 ? null : JsonSerializer.Serialize(oldValues),
                NewValues = newValues.Count == 0 ? null : JsonSerializer.Serialize(newValues)
            });
        }
    }

    private static bool IsSensitive(string name) => name is "PasswordHash" or "SecurityStamp" or "ConcurrencyStamp" or "AuthenticatorKey" or "Value";

    private static string? FindActor(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry, string action) =>
        entry.Properties.FirstOrDefault(property => property.Metadata.Name == (action == "Created" ? "CreatedBy" : "UpdatedBy"))?.CurrentValue?.ToString()
        ?? "system";

    private static string FindLabel(IReadOnlyDictionary<string, object?> values, string fallback)
    {
        foreach (var name in new[] { "Name", "DisplayName", "Description", "Text", "Email", "SeriesNumber", "Reference", "Key", "Date" })
            if (values.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value?.ToString())) return value!.ToString()!;
        return fallback;
    }

    /// <summary>
    /// Applies audit timestamps to entities that implement <see cref="AuditableEntity"/>.
    /// Sets CreatedAt for new entities and UpdatedAt for modified entities.
    /// </summary>
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

    /// <summary>
    /// Validates that all journal entries are balanced before saving.
    /// Ensures that debits equal credits for each journal entry.
    /// </summary>
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
