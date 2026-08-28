using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Application.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace DenariusAI.Infrastructure.Persistence;

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
        Validate(document, entityTypes);
        var records = document.Tables.Sum(table => table.Value.Count);

        async Task RestoreOperation()
        {
            dbContext.ChangeTracker.Clear();
            dbContext.SuppressAudit = true;
            try
            {
                var existing = entityTypes.Values.SelectMany(entityType => Query(entityType.ClrType).Cast<object>()).ToList();
                dbContext.RemoveRange(existing);
                await dbContext.SaveChangesAsync(cancellationToken);
                dbContext.ChangeTracker.Clear();

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
}
