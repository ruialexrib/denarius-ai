using DenariusAI.Infrastructure.Identity;
using DenariusAI.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json.Nodes;

namespace DenariusAI.IntegrationTests;

public sealed class ApplicationBackupServiceTests
{
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
        await context.SaveChangesAsync();
        var service = new ApplicationBackupService(context);

        var backup = await service.ExportAsync("0.19.0");
        var legacyBackup = JsonNode.Parse(backup)!.AsObject();
        foreach (var table in legacyBackup["tables"]!.AsObject())
        foreach (var row in table.Value!.AsArray())
            row!.AsObject().Remove(nameof(ApplicationUser.ShowAssetBalancesWidget));
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
    }

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
