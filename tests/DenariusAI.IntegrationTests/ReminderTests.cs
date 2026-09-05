using DenariusAI.Domain.Entities;
using DenariusAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DenariusAI.IntegrationTests;

public sealed class ReminderTests
{
    [Fact]
    public async Task AvailableReminderRemainsActiveUntilEachUserAcknowledgesIt()
    {
        await using var context = new DenariusDbContext(new DbContextOptionsBuilder<DenariusDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        await context.Database.EnsureCreatedAsync();
        var reminder = new Reminder("Pagar seguro", DateOnly.FromDateTime(DateTime.Today.AddDays(2)), 5);
        context.Reminders.Add(reminder); await context.SaveChangesAsync();
        context.ReminderAcknowledgements.Add(new ReminderAcknowledgement { ReminderId = reminder.Id, UserId = "user-a", AcknowledgedAt = DateTimeOffset.UtcNow });
        await context.SaveChangesAsync();

        Assert.False(await context.Reminders.AnyAsync(item => item.Id == reminder.Id && !item.Acknowledgements.Any(value => value.UserId == "user-a")));
        Assert.True(await context.Reminders.AnyAsync(item => item.Id == reminder.Id && !item.Acknowledgements.Any(value => value.UserId == "user-b")));
    }

    [Fact]
    public void AvailabilityQueryCanBeTranslatedForSqlServer()
    {
        using var context = new DenariusDbContext(new DbContextOptionsBuilder<DenariusDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=translation-only;Trusted_Connection=True").Options);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var sql = context.Reminders.Where(item => item.EventDate.AddDays(-item.NoticeDays) <= today).ToQueryString();
        Assert.Contains("DATEADD", sql, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Verifies the shared active-alerts query (used by the dashboard and the top navigation icon)
    /// excludes reminders outside their notice window as well as reminders already acknowledged by the user.</summary>
    [Fact]
    public async Task ActiveAlertsExcludesFutureAndAcknowledgedReminders()
    {
        await using var context = new DenariusDbContext(new DbContextOptionsBuilder<DenariusDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        await context.Database.EnsureCreatedAsync();
        var today = DateOnly.FromDateTime(DateTime.Today);
        var overdue = new Reminder("Renovar seguro", today.AddDays(-1), 3);
        var upcomingButNotYetNoticed = new Reminder("Pagar IMI", today.AddDays(30), 3);
        var acknowledged = new Reminder("Rever orçamento", today, 0);
        context.AddRange(overdue, upcomingButNotYetNoticed, acknowledged);
        await context.SaveChangesAsync();
        context.ReminderAcknowledgements.Add(new ReminderAcknowledgement { ReminderId = acknowledged.Id, UserId = "user-a", AcknowledgedAt = DateTimeOffset.UtcNow });
        await context.SaveChangesAsync();

        var ownReminderIds = new[] { overdue.Id, upcomingButNotYetNoticed.Id, acknowledged.Id };
        var alerts = await context.ActiveAlerts("user-a", today).Where(item => ownReminderIds.Contains(item.Id))
            .OrderBy(item => item.EventDate).Select(item => item.Id).ToListAsync();

        Assert.Single(alerts);
        Assert.Equal(overdue.Id, alerts[0]);
    }

    /// <summary>Verifies the shared active-alerts query remains translatable to SQL Server.</summary>
    [Fact]
    public void ActiveAlertsQueryCanBeTranslatedForSqlServer()
    {
        using var context = new DenariusDbContext(new DbContextOptionsBuilder<DenariusDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=translation-only;Trusted_Connection=True").Options);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var sql = context.ActiveAlerts("user-a", today).ToQueryString();
        Assert.Contains("DATEADD", sql, StringComparison.OrdinalIgnoreCase);
    }
}
