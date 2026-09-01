using DenariusAI.Infrastructure.Identity;
using DenariusAI.Domain.Entities;
using DenariusAI.Domain.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace DenariusAI.Infrastructure.Persistence;

/// <summary>Database context for the Denarius AI application.</summary>
public sealed class DenariusDbContext(DbContextOptions<DenariusDbContext> options, IHttpContextAccessor? httpContextAccessor = null) : IdentityDbContext<ApplicationUser>(options)
{
    /// <summary>Gets or sets whether audit capture is temporarily suppressed.</summary>
    public bool SuppressAudit { get; set; }
    /// <summary>Gets financial groups.</summary>
    public DbSet<FinancialGroup> FinancialGroups => Set<FinancialGroup>();
    /// <summary>Gets categories.</summary>
    public DbSet<Category> Categories => Set<Category>();
    /// <summary>Gets accounts.</summary>
    public DbSet<Account> Accounts => Set<Account>();
    /// <summary>Gets journal entries.</summary>
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    /// <summary>Gets journal entry lines.</summary>
    public DbSet<JournalEntryLine> JournalEntryLines => Set<JournalEntryLine>();
    /// <summary>Gets budgets.</summary>
    public DbSet<Budget> Budgets => Set<Budget>();
    /// <summary>Gets budget lines.</summary>
    public DbSet<BudgetLine> BudgetLines => Set<BudgetLine>();
    /// <summary>Gets reconciliations.</summary>
    public DbSet<Reconciliation> Reconciliations => Set<Reconciliation>();
    /// <summary>Gets application settings.</summary>
    public DbSet<ApplicationSetting> ApplicationSettings => Set<ApplicationSetting>();
    /// <summary>Gets savings certificates.</summary>
    public DbSet<SavingsCertificate> SavingsCertificates => Set<SavingsCertificate>();
    /// <summary>Gets stock positions.</summary>
    public DbSet<StockPosition> StockPositions => Set<StockPosition>();
    /// <summary>Gets stock price observations.</summary>
    public DbSet<StockPrice> StockPrices => Set<StockPrice>();
    /// <summary>Gets reminders.</summary>
    public DbSet<Reminder> Reminders => Set<Reminder>();
    /// <summary>Gets reminder acknowledgements.</summary>
    public DbSet<ReminderAcknowledgement> ReminderAcknowledgements => Set<ReminderAcknowledgement>();
    /// <summary>Gets warranties.</summary>
    public DbSet<Warranty> Warranties => Set<Warranty>();
    /// <summary>Gets correspondence.</summary>
    public DbSet<Correspondence> Correspondence => Set<Correspondence>();
    /// <summary>Gets correspondence metadata.</summary>
    public DbSet<CorrespondenceMetadata> CorrespondenceMetadata => Set<CorrespondenceMetadata>();
    /// <summary>Gets insurance policies.</summary>
    public DbSet<InsurancePolicy> InsurancePolicies => Set<InsurancePolicy>();
    /// <summary>Gets general insurance policy attachments.</summary>
    public DbSet<InsurancePolicyAttachment> InsurancePolicyAttachments => Set<InsurancePolicyAttachment>();
    /// <summary>Gets insurance premiums.</summary>
    public DbSet<InsurancePremium> InsurancePremiums => Set<InsurancePremium>();
    /// <summary>Gets insurance premium attachments.</summary>
    public DbSet<InsurancePremiumAttachment> InsurancePremiumAttachments => Set<InsurancePremiumAttachment>();
    /// <summary>Gets audit logs.</summary>
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    /// <summary>Gets login history.</summary>
    public DbSet<LoginHistory> LoginHistory => Set<LoginHistory>();

    /// <summary>Configures entity mappings.</summary>
    /// <param name="builder">Model builder.</param>
    protected override void OnModelCreating(ModelBuilder builder) { base.OnModelCreating(builder); builder.HasDefaultSchema("denarius"); builder.ApplyConfigurationsFromAssembly(typeof(DenariusDbContext).Assembly); }

    /// <summary>Saves tracked changes after applying audit and accounting validation.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of written state entries.</returns>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) { ApplyAuditTimestamps(); ValidateJournalEntries(); CaptureAuditLogs(); return base.SaveChangesAsync(cancellationToken); }

    /// <summary>Captures audit log entries for tracked changes.</summary>
    private void CaptureAuditLogs()
    {
        if (SuppressAudit) return;
        var entries = ChangeTracker.Entries().Where(entry => entry.Entity is not AuditLog && entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted && (entry.Entity is AuditableEntity || entry.Entity is ApplicationUser or ReminderAcknowledgement)).ToList();
        if (entries.Count == 0) return;
        var principal = httpContextAccessor?.HttpContext?.User; var userId = principal?.FindFirstValue(ClaimTypes.NameIdentifier); var userName = principal?.Identity?.Name; var now = DateTimeOffset.UtcNow;
        foreach (var entry in entries)
        {
            var key = string.Join("|", entry.Properties.Where(property => property.Metadata.IsPrimaryKey()).Select(property => (entry.State == EntityState.Deleted ? property.OriginalValue : property.CurrentValue)?.ToString())); if (string.IsNullOrWhiteSpace(key)) continue;
            var oldValues = new SortedDictionary<string, object?>(); var newValues = new SortedDictionary<string, object?>(); var changed = new List<string>();
            foreach (var property in entry.Properties)
            {
                if (IsSensitive(property.Metadata.Name)) { if (entry.State == EntityState.Modified && property.IsModified && !Equals(property.OriginalValue, property.CurrentValue)) changed.Add(property.Metadata.Name); continue; }
                if (entry.State is EntityState.Modified or EntityState.Deleted) oldValues[property.Metadata.Name] = property.OriginalValue; if (entry.State is EntityState.Added or EntityState.Modified) newValues[property.Metadata.Name] = property.CurrentValue; if (entry.State == EntityState.Modified && property.IsModified && !Equals(property.OriginalValue, property.CurrentValue)) changed.Add(property.Metadata.Name);
            }
            if (entry.State == EntityState.Modified && changed.Count == 0) continue; var action = entry.State switch { EntityState.Added => "Created", EntityState.Modified => "Updated", _ => "Deleted" }; var valuesForLabel = entry.State == EntityState.Deleted ? oldValues : newValues;
            AuditLogs.Add(new AuditLog { EntityType = entry.Metadata.ClrType.Name, EntityId = key, RecordLabel = FindLabel(valuesForLabel, key), Action = action, ChangedAt = now, UserId = userId ?? FindActor(entry, action), UserName = userName, ChangedColumns = changed.Count == 0 ? null : JsonSerializer.Serialize(changed), OldValues = oldValues.Count == 0 ? null : JsonSerializer.Serialize(oldValues), NewValues = newValues.Count == 0 ? null : JsonSerializer.Serialize(newValues) });
        }
    }

    /// <summary>Determines whether an audited property contains sensitive data.</summary>
    /// <param name="name">Property name.</param><returns>True for sensitive properties.</returns>
    private static bool IsSensitive(string name) => name is "PasswordHash" or "SecurityStamp" or "ConcurrencyStamp" or "AuthenticatorKey" or "Value" or "ProfileImageBase64" or "DocumentBase64";
    /// <summary>Finds the actor for an audited entry.</summary><param name="entry">Tracked entry.</param><param name="action">Audit action.</param><returns>Actor identifier.</returns>
    private static string? FindActor(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry, string action) => entry.Properties.FirstOrDefault(property => property.Metadata.Name == (action == "Created" ? "CreatedBy" : "UpdatedBy"))?.CurrentValue?.ToString() ?? "system";
    /// <summary>Finds a human-readable audit label.</summary><param name="values">Property values.</param><param name="fallback">Fallback label.</param><returns>Record label.</returns>
    private static string FindLabel(IReadOnlyDictionary<string, object?> values, string fallback) { foreach (var name in new[] { "Name", "Ticker", "Subject", "DisplayName", "Description", "Text", "Email", "SeriesNumber", "PolicyNumber", "Reference", "Key", "Date" }) if (values.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value?.ToString())) return value!.ToString()!; return fallback; }
    /// <summary>Applies audit timestamps.</summary>
    private void ApplyAuditTimestamps() { var now = DateTimeOffset.UtcNow; foreach (var trackedEntry in ChangeTracker.Entries<AuditableEntity>()) { if (trackedEntry.State == EntityState.Added && trackedEntry.Entity.CreatedAt == default) trackedEntry.Entity.CreatedAt = now; if (trackedEntry.State == EntityState.Modified) trackedEntry.Entity.UpdatedAt = now; } }
    /// <summary>Validates affected journal entries.</summary>
    private void ValidateJournalEntries() { var entries = ChangeTracker.Entries<JournalEntry>().Where(entry => entry.State is EntityState.Added or EntityState.Modified).Select(entry => entry.Entity).Concat(ChangeTracker.Entries<JournalEntryLine>().Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).Select(entry => entry.Entity.JournalEntry)).Where(entry => entry is not null).Distinct(); foreach (var entry in entries) entry.EnsureBalanced(); }
}
