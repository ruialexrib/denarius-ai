using DenariusAI.Domain.Entities;
using DenariusAI.Domain.Enums;
using DenariusAI.Infrastructure.Identity;
using DenariusAI.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json.Nodes;

namespace DenariusAI.IntegrationTests;

/// <summary>Verifies full application backup export, compatibility, and restore behavior.</summary>
public sealed class ApplicationBackupServiceTests
{
    /// <summary>Verifies a current backup restores all mapped data and safely upgrades an older schema.</summary>
    [Fact]
    public async Task ExportAndRestoreReplaceAllMappedRecords()
    {
        var options = new DbContextOptionsBuilder<DenariusDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new DenariusDbContext(options);
        var user = new ApplicationUser { Id = "admin-1", UserName = "admin@test.local", NormalizedUserName = "ADMIN@TEST.LOCAL", Email = "admin@test.local", NormalizedEmail = "ADMIN@TEST.LOCAL", DisplayName = "Administrador", PasswordHash = "hash" };
        context.Users.Add(user);
        context.Roles.Add(new IdentityRole { Id = "role-1", Name = "Administrator", NormalizedName = "ADMINISTRATOR" });
        context.UserRoles.Add(new IdentityUserRole<string> { UserId = user.Id, RoleId = "role-1" });
        context.ApplicationSettings.Add(new() { Key = "Prompts.Test", Value = "original", CreatedBy = user.Id });
        var bank = new Account { Name = "Conta", AccountType = AccountType.BankAccount };
        var expense = new Account { Name = "Despesa", AccountType = AccountType.Expense };
        var entry = new JournalEntry(new DateOnly(2026, 8, 29), "Compra");
        entry.AddLine(expense.Id, 25m, 0m); entry.AddLine(bank.Id, 0m, 25m);
        var certificate = new SavingsCertificate(new DateOnly(2026, 1, 1), "F-RESTORE", "Teste", 100m, 2m, 101m, new DateOnly(2026, 10, 1));
        var capitalizationReminder = new Reminder("Capitalização do Certificado de Aforro F-RESTORE: Teste", certificate.NextCapitalization, 7);
        capitalizationReminder.LinkToSavingsCertificate(certificate.Id);
        context.AddRange(bank, expense, entry, certificate, capitalizationReminder);
        await context.SaveChangesAsync();
        var service = new ApplicationBackupService(context);

        var backup = await service.ExportAsync("0.19.0");
        var legacyBackup = JsonNode.Parse(backup)!.AsObject();
        foreach (var table in legacyBackup["tables"]!.AsObject())
        foreach (var row in table.Value!.AsArray())
            row!.AsObject().Remove(nameof(ApplicationUser.ShowAssetBalancesWidget));
        var tables = legacyBackup["tables"]!.AsObject();
        tables.Remove(typeof(Correspondence).FullName!); tables.Remove(typeof(Warranty).FullName!); tables.Remove(typeof(CorrespondenceMetadata).FullName!);
        tables.Remove(typeof(InsurancePolicy).FullName!); tables.Remove(typeof(InsurancePolicyAttachment).FullName!); tables.Remove(typeof(InsurancePremium).FullName!); tables.Remove(typeof(InsurancePremiumAttachment).FullName!);
        foreach (var row in tables[typeof(Reminder).FullName!]!.AsArray())
        {
            row!.AsObject().Remove(nameof(Reminder.SavingsCertificateId));
            row.AsObject().Remove(nameof(Reminder.WarrantyId));
        }
        backup = Encoding.UTF8.GetBytes(legacyBackup.ToJsonString());
        context.ApplicationSettings.RemoveRange(context.ApplicationSettings);
        user.DisplayName = "Alterado";
        context.ApplicationSettings.Add(new() { Key = "Temporary", Value = "remove", CreatedBy = user.Id });
        await context.SaveChangesAsync();

        await using var stream = new MemoryStream(backup);
        var result = await service.RestoreAsync(stream);

        Assert.True(result.Tables > 10);
        Assert.Equal("Administrador", (await context.Users.SingleAsync()).DisplayName);
        Assert.True((await context.Users.SingleAsync()).ShowAssetBalancesWidget);
        var userRole = await context.UserRoles.SingleAsync();
        Assert.Equal("admin-1", userRole.UserId);
        Assert.Equal("role-1", userRole.RoleId);
        var setting = await context.ApplicationSettings.SingleAsync();
        Assert.Equal("Prompts.Test", setting.Key);
        Assert.Equal("original", setting.Value);
        Assert.Equal(2, await context.JournalEntryLines.CountAsync());
        Assert.Single(await context.Reminders.Where(item => item.SavingsCertificateId == certificate.Id).ToListAsync());
        Assert.Empty(context.InsurancePolicies);
        Assert.Empty(context.InsurancePolicyAttachments);
        Assert.Empty(context.InsurancePremiums);
        Assert.Empty(context.InsurancePremiumAttachments);
    }

    /// <summary>Verifies an invalid backup is rejected before existing data is changed.</summary>
    [Fact]
    public async Task RestoreRejectsUnknownFormatWithoutChangingData()
    {
        var options = new DbContextOptionsBuilder<DenariusDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new DenariusDbContext(options);
        context.ApplicationSettings.Add(new() { Key = "Keep", Value = "value", CreatedBy = "test" });
        await context.SaveChangesAsync();
        var service = new ApplicationBackupService(context);
        await using var stream = new MemoryStream("{\"format\":\"Unknown\",\"schemaVersion\":1,\"tables\":{}}"u8.ToArray());

        await Assert.ThrowsAsync<InvalidDataException>(() => service.RestoreAsync(stream));

        Assert.Equal("Keep", (await context.ApplicationSettings.SingleAsync()).Key);
    }
}
