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
}
