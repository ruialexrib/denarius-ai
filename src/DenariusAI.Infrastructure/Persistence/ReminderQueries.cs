using DenariusAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DenariusAI.Infrastructure.Persistence;

/// <summary>
/// Provides reusable, translatable queries over <see cref="Reminder"/> entities so that reminder
/// availability rules are defined once and shared across the dashboard, the top navigation bar and
/// the reminders area.
/// </summary>
public static class ReminderQueries
{
    /// <summary>
    /// Builds a query for reminders that are currently available (their notice window has started)
    /// and have not yet been acknowledged by the specified user.
    /// </summary>
    /// <param name="dbContext">The database context to query.</param>
    /// <param name="userId">The identifier of the user whose acknowledgements are considered.</param>
    /// <param name="today">The current date used to evaluate the notice window.</param>
    /// <returns>A queryable of reminders requiring the user's attention, ordered is left to the caller.</returns>
    public static IQueryable<Reminder> ActiveAlerts(this DenariusDbContext dbContext, string userId, DateOnly today) =>
        dbContext.Reminders.AsNoTracking()
            .Where(item => item.EventDate.AddDays(-item.NoticeDays) <= today && !item.Acknowledgements.Any(value => value.UserId == userId));
}
