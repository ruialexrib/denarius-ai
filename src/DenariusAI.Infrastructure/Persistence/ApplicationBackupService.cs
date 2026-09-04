using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Application.DTOs;
using DenariusAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace DenariusAI.Infrastructure.Persistence;

/// <summary>Exports and restores complete, versioned application data backups.</summary>
/// <param name="dbContext">Application database context.</param>
public sealed class ApplicationBackupService(DenariusDbContext dbContext) : IApplicationBackupService
{
    private const string Format = "DenariusAI.ApplicationBackup";
    private const int SchemaVersion = 1;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private static readonly MethodInfo SetMethod = typeof(DbContext).GetMethods()
        .Single(method => method.Name == nameof(DbContext.Set) && method.IsGenericMethodDefinition && method.GetParameters().Length == 0);

    public Task<byte[]> ExportAsync(string applicationVersion, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var tables = new Dictionary<string, List<Dictionary<string, JsonElement>>>(StringComparer.Ordinal);
        foreach (var entityType in EntityTypes())
        {
            var rows = new List<Dictionary<string, JsonElement>>();
            foreach (var entity in Query(entityType.ClrType))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var entry = dbContext.Entry(entity!);
                var row = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
                foreach (var property in entityType.GetProperties().OrderBy(property => property.Name))
                    row[property.Name] = JsonSerializer.SerializeToElement(entry.Property(property.Name).CurrentValue, property.ClrType, JsonOptions);
                rows.Add(row);
            }
            tables[EntityKey(entityType)] = rows;
        }
        var document = new ApplicationBackupDto(Format, SchemaVersion, DateTimeOffset.UtcNow, applicationVersion, tables);
        return Task.FromResult(JsonSerializer.SerializeToUtf8Bytes(document, JsonOptions));
    }

    public async Task<ApplicationRestoreResult> RestoreAsync(Stream json, CancellationToken cancellationToken = default)
    {
        ApplicationBackupDto document;
        try { document = await JsonSerializer.DeserializeAsync<ApplicationBackupDto>(json, JsonOptions, cancellationToken) ?? throw new InvalidDataException("O ficheiro está vazio."); }
        catch (JsonException exception) { throw new InvalidDataException("O ficheiro não contém um backup JSON válido.", exception); }

        var entityTypes = EntityTypes().ToDictionary(EntityKey, StringComparer.Ordinal);
        NormalizeForCurrentSchema(document, entityTypes);
        Validate(document, entityTypes);
        var records = document.Tables.Sum(table => table.Value.Count);

        async Task RestoreOperation()
        {
            dbContext.ChangeTracker.Clear();
            dbContext.SuppressAudit = true;
            try
            {
                foreach (var entityType in DeleteOrder(entityTypes.Values))
                {
                    var existing = Query(entityType.ClrType).Cast<object>().ToList();
                    if (existing.Count == 0) continue;
                    dbContext.RemoveRange(existing);
                    await dbContext.SaveChangesAsync(cancellationToken);
                    dbContext.ChangeTracker.Clear();
                }

                foreach (var entityType in entityTypes.Values)
                foreach (var row in document.Tables[EntityKey(entityType)])
                {
                    var entity = Create(entityType.ClrType);
                    var entry = dbContext.Entry(entity);
                    foreach (var property in entityType.GetProperties())
                    {
                        if (!row.TryGetValue(property.Name, out var serializedValue)) continue;
                        var value = serializedValue.Deserialize(property.ClrType, JsonOptions);
                        entry.Property(property.Name).CurrentValue = value;
                    }
                    entry.State = EntityState.Added;
                }
                await dbContext.SaveChangesAsync(cancellationToken);
                await EnsureLinkedRemindersAsync(cancellationToken);
            }
            finally { dbContext.SuppressAudit = false; }
        }

        if (!dbContext.Database.IsRelational()) await RestoreOperation();
        else
        {
            var strategy = dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
                await RestoreOperation();
                await transaction.CommitAsync(cancellationToken);
            });
        }
        return new(document.Tables.Count, records);
    }

    private IReadOnlyList<IEntityType> EntityTypes() => dbContext.Model.GetEntityTypes()
        .Where(entityType => !entityType.IsOwned() && entityType.ClrType is not null)
        .OrderBy(EntityKey, StringComparer.Ordinal).ToList();

    private IEnumerable Query(Type type) => (IEnumerable)SetMethod.MakeGenericMethod(type).Invoke(dbContext, null)!;
    private static string EntityKey(IEntityType entityType) => entityType.ClrType.FullName ?? entityType.Name;
    private static IReadOnlyList<IEntityType> DeleteOrder(IEnumerable<IEntityType> entityTypes)
    {
        var types = entityTypes.ToList();
        var typeSet = types.ToHashSet();
        var depths = new Dictionary<IEntityType, int>();

        int Depth(IEntityType entityType, HashSet<IEntityType> path)
        {
            if (depths.TryGetValue(entityType, out var known)) return known;
            if (!path.Add(entityType)) return 0;
            var principalDepth = entityType.GetForeignKeys()
                .Where(foreignKey => typeSet.Contains(foreignKey.PrincipalEntityType))
                .Select(foreignKey => Depth(foreignKey.PrincipalEntityType, path) + 1)
                .DefaultIfEmpty(0).Max();
            path.Remove(entityType);
            depths[entityType] = principalDepth;
            return principalDepth;
        }

        return types.OrderByDescending(entityType => Depth(entityType, []))
            .ThenBy(EntityKey, StringComparer.Ordinal).ToList();
    }
    private static object Create(Type type)
    {
        try { return Activator.CreateInstance(type, nonPublic: true) ?? RuntimeHelpers.GetUninitializedObject(type); }
        catch (MissingMethodException) { return RuntimeHelpers.GetUninitializedObject(type); }
    }

    private static void Validate(ApplicationBackupDto document, IReadOnlyDictionary<string, IEntityType> entityTypes)
    {
        if (document.Format != Format || document.SchemaVersion != SchemaVersion)
            throw new InvalidDataException("O formato ou a versão deste backup não é suportado.");
        if (document.Tables.Count != entityTypes.Count || document.Tables.Keys.Any(key => !entityTypes.ContainsKey(key)))
            throw new InvalidDataException("O backup não contém todas as tabelas exigidas por esta versão da aplicação.");
        foreach (var pair in entityTypes)
        {
            if (!document.Tables.TryGetValue(pair.Key, out var rows)) throw new InvalidDataException($"Falta a tabela {pair.Key}.");
            var properties = pair.Value.GetProperties().Select(property => property.Name).ToHashSet(StringComparer.Ordinal);
            var optionalProperties = pair.Value.ClrType == typeof(DenariusAI.Infrastructure.Identity.ApplicationUser)
                ? new HashSet<string>(
                [
                    nameof(DenariusAI.Infrastructure.Identity.ApplicationUser.ShowAssetBalancesWidget),
                    nameof(DenariusAI.Infrastructure.Identity.ApplicationUser.ProfileImageBase64),
                    nameof(DenariusAI.Infrastructure.Identity.ApplicationUser.ProfileImageContentType)
                ], StringComparer.Ordinal)
                : [];
            if (rows.Any(row => row.Keys.Any(key => !properties.Contains(key)) || properties.Except(row.Keys).Any(property => !optionalProperties.Contains(property))))
                throw new InvalidDataException($"A estrutura da tabela {pair.Key} é inválida.");
        }
    }

    /// <summary>Adds safe defaults required to restore backups created by earlier application schemas.</summary>
    /// <param name="document">Backup document to normalize.</param>
    /// <param name="entityTypes">Entity types required by the current schema.</param>
    private static void NormalizeForCurrentSchema(ApplicationBackupDto document, IReadOnlyDictionary<string, IEntityType> entityTypes)
    {
        foreach (var type in new[]
        {
            typeof(Correspondence),
            typeof(Warranty),
            typeof(CorrespondenceMetadata),
            typeof(InsurancePolicy),
            typeof(InsurancePolicyAttachment),
            typeof(InsurancePremium),
            typeof(InsurancePremiumAttachment),
            typeof(StockPosition),
            typeof(StockPrice)
        })
        {
            var key = type.FullName!;
            if (entityTypes.ContainsKey(key) && !document.Tables.ContainsKey(key)) document.Tables[key] = [];
        }

        var reminderKey = typeof(Reminder).FullName!;
        if (!document.Tables.TryGetValue(reminderKey, out var reminders)) return;
        foreach (var row in reminders)
        {
            if (!row.ContainsKey(nameof(Reminder.SavingsCertificateId))) row[nameof(Reminder.SavingsCertificateId)] = JsonSerializer.SerializeToElement<Guid?>(null);
            if (!row.ContainsKey(nameof(Reminder.WarrantyId))) row[nameof(Reminder.WarrantyId)] = JsonSerializer.SerializeToElement<Guid?>(null);
        }
    }

    private async Task EnsureLinkedRemindersAsync(CancellationToken cancellationToken)
    {
        var linkedCertificates = await dbContext.Reminders.Where(item => item.SavingsCertificateId != null)
            .Select(item => item.SavingsCertificateId!.Value).ToHashSetAsync(cancellationToken);
        foreach (var certificate in await dbContext.SavingsCertificates.Where(item => !linkedCertificates.Contains(item.Id)).ToListAsync(cancellationToken))
        {
            var reminder = new Reminder($"Capitalização do Certificado de Aforro {certificate.SeriesNumber}: {certificate.Description}", certificate.NextCapitalization, 7) { CreatedBy = "system" };
            reminder.LinkToSavingsCertificate(certificate.Id); dbContext.Reminders.Add(reminder);
        }

        var linkedWarranties = await dbContext.Reminders.Where(item => item.WarrantyId != null)
            .Select(item => item.WarrantyId!.Value).ToHashSetAsync(cancellationToken);
        foreach (var warranty in await dbContext.Warranties.Where(item => !linkedWarranties.Contains(item.Id)).ToListAsync(cancellationToken))
        {
            var reminder = new Reminder($"Fim da garantia: {warranty.Name}", warranty.ExpiryDate, 30) { CreatedBy = "system" };
            reminder.LinkToWarranty(warranty.Id); dbContext.Reminders.Add(reminder);
        }
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
