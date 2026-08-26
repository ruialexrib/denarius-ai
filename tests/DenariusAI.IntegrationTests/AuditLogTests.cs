using System.Security.Claims;
using System.Text.Json;
using DenariusAI.Domain.Entities;
using DenariusAI.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace DenariusAI.IntegrationTests;

/// <summary>
/// Contains definitions for AuditLogTests.
/// </summary>
public sealed class AuditLogTests
{
    [Fact]
    public async Task SaveChanges_records_create_update_and_delete_with_the_current_user()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "admin-id"),
            new Claim(ClaimTypes.Name, "admin@denarius.local")
        ], "test"));
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = principal } };
        await using var context = CreateContext(accessor);
        var reminder = new Reminder("Renovar seguro", new DateOnly(2026, 9, 15), 15);

        context.Reminders.Add(reminder);
        await context.SaveChangesAsync();
        reminder.Update("Renovar seguro automóvel", new DateOnly(2026, 9, 20), 20);
        await context.SaveChangesAsync();
        context.Reminders.Remove(reminder);
        await context.SaveChangesAsync();

        var logs = await context.AuditLogs.Where(x => x.EntityId == reminder.Id.ToString())
            .OrderBy(x => x.ChangedAt).ToListAsync();
        Assert.Equal(["Created", "Updated", "Deleted"], logs.Select(x => x.Action));
        Assert.All(logs, log =>
        {
            Assert.Equal("Reminder", log.EntityType);
            Assert.Equal("admin-id", log.UserId);
            Assert.Equal("admin@denarius.local", log.UserName);
        });
        Assert.Contains("Text", logs[1].ChangedColumns);
        Assert.Equal("Renovar seguro", JsonDocument.Parse(logs[1].OldValues!).RootElement.GetProperty("Text").GetString());
        Assert.Equal("Renovar seguro automóvel", JsonDocument.Parse(logs[1].NewValues!).RootElement.GetProperty("Text").GetString());
    }

    [Fact]
    public async Task Audit_does_not_store_sensitive_setting_values()
    {
        await using var context = CreateContext();
        var setting = new ApplicationSetting { Key = "MistralApiKey", Value = "top-secret" };

        context.ApplicationSettings.Add(setting);
        await context.SaveChangesAsync();

        var log = await context.AuditLogs.SingleAsync(x => x.EntityId == setting.Id.ToString());
        Assert.DoesNotContain("top-secret", log.NewValues ?? string.Empty);
        Assert.DoesNotContain("Value", log.NewValues ?? string.Empty);

        setting.Value = "new-secret";
        await context.SaveChangesAsync();
        var updated = await context.AuditLogs.SingleAsync(x => x.EntityId == setting.Id.ToString() && x.Action == "Updated");
        Assert.Contains("Value", updated.ChangedColumns);
        Assert.DoesNotContain("new-secret", updated.NewValues ?? string.Empty);
    }

    private static DenariusDbContext CreateContext(IHttpContextAccessor? accessor = null) => new(
        new DbContextOptionsBuilder<DenariusDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
        accessor);
}
