using DenariusAI.Web.Models;
using DenariusAI.Web.ViewModels;

namespace DenariusAI.IntegrationTests;

/// <summary>
/// Verifies reminder calendar PDF generation.
/// </summary>
public sealed class ReminderCalendarPdfTests
{
    /// <summary>
    /// Verifies that the selected month can be rendered as a valid PDF document.
    /// </summary>
    [Fact]
    public void GenerateCreatesPdfForSelectedMonth()
    {
        var monthStart = new DateOnly(2026, 9, 1);
        var today = new DateOnly(2026, 9, 10);
        var reminders = new List<ReminderRowViewModel>
        {
            new(Guid.NewGuid(), "Pagar eletricidade", new DateOnly(2026, 9, 10), 3, true, false, 0),
            new(Guid.NewGuid(), "Renovar seguro", new DateOnly(2026, 9, 18), 10, true, true, 8),
            new(Guid.NewGuid(), "Evento futuro", new DateOnly(2026, 9, 29), 2, false, false, 19)
        };

        var pdf = ReminderCalendarPdf.Generate(monthStart, reminders, today);

        Assert.True(pdf.Length > 4);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
    }
}
