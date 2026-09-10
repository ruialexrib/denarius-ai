using System.Globalization;
using DenariusAI.Web.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace DenariusAI.Web.Models;

/// <summary>
/// Generates landscape monthly calendar PDFs for reminders.
/// </summary>
public static class ReminderCalendarPdf
{
    private const string Ink = "#17243A";
    private const string Muted = "#607086";
    private const string Accent = "#159A70";
    private const string Grid = "#DCE5E9";
    private const string Adjacent = "#F5F7F8";
    private const string Today = "#EEF7F4";
    private const int MaximumVisibleRemindersPerDay = 3;

    /// <summary>
    /// Generates a one-page A4 landscape calendar for the selected month.
    /// </summary>
    /// <param name="monthStart">The first day of the selected month.</param>
    /// <param name="reminders">The reminders to render.</param>
    /// <param name="today">The current date used to identify today's cell.</param>
    /// <returns>The generated PDF document bytes.</returns>
    public static byte[] Generate(DateOnly monthStart, IReadOnlyList<ReminderRowViewModel> reminders, DateOnly today)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var culture = CultureInfo.GetCultureInfo("pt-PT");
        var normalizedMonth = new DateOnly(monthStart.Year, monthStart.Month, 1);
        var period = UpperFirst(normalizedMonth.ToString("MMMM 'de' yyyy", culture));
        var firstCalendarDay = normalizedMonth.AddDays(-(((int)normalizedMonth.DayOfWeek + 6) % 7));
        var calendarDays = Enumerable.Range(0, 42).Select(offset => firstCalendarDay.AddDays(offset)).ToArray();
        var remindersByDay = reminders
            .Where(item => item.EventDate.Year == normalizedMonth.Year && item.EventDate.Month == normalizedMonth.Month)
            .GroupBy(item => item.EventDate)
            .ToDictionary(group => group.Key, group => group.OrderBy(item => item.Text).ToList());

        return Document.Create(document => document.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(24);
            page.DefaultTextStyle(style => style.FontSize(8).FontColor(Ink).LineHeight(1.15f));
            RenderHeader(page.Header(), period);
            page.Content().PaddingTop(10).Table(table => RenderCalendar(table, normalizedMonth, calendarDays, remindersByDay, today));
            RenderFooter(page.Footer());
        })).GeneratePdf();
    }

    /// <summary>
    /// Renders the document header.
    /// </summary>
    /// <param name="container">The target header container.</param>
    /// <param name="period">The selected month label.</param>
    private static void RenderHeader(IContainer container, string period) => container.Row(row =>
    {
        row.RelativeItem().Column(column =>
        {
            column.Item().Text("DENARIUSAI").FontSize(8).Bold().FontColor(Accent).LetterSpacing(.08f);
            column.Item().PaddingTop(2).Text("Calendário de lembretes").FontSize(17).Bold();
        });
        row.ConstantItem(190).AlignRight().Column(column =>
        {
            column.Item().AlignRight().Text("MÊS SELECIONADO").FontSize(7).Bold().FontColor(Muted);
            column.Item().AlignRight().PaddingTop(3).Text(period).FontSize(10).SemiBold();
        });
    });

    /// <summary>
    /// Renders the seven-column monthly calendar grid.
    /// </summary>
    /// <param name="table">The calendar table descriptor.</param>
    /// <param name="monthStart">The first day of the selected month.</param>
    /// <param name="calendarDays">The 42 dates displayed in the calendar grid.</param>
    /// <param name="remindersByDay">The selected month's reminders grouped by event date.</param>
    /// <param name="today">The current date.</param>
    private static void RenderCalendar(
        TableDescriptor table,
        DateOnly monthStart,
        IReadOnlyList<DateOnly> calendarDays,
        IReadOnlyDictionary<DateOnly, List<ReminderRowViewModel>> remindersByDay,
        DateOnly today)
    {
        table.ColumnsDefinition(columns =>
        {
            for (var column = 0; column < 7; column++)
            {
                columns.RelativeColumn();
            }
        });

        foreach (var weekday in new[] { "SEG", "TER", "QUA", "QUI", "SEX", "SÁB", "DOM" })
        {
            table.Cell().Background("#EDF5F2").Border(0.5f).BorderColor(Grid).Padding(5)
                .AlignCenter().Text(weekday).FontSize(7).Bold().FontColor(Muted);
        }

        foreach (var day in calendarDays)
        {
            remindersByDay.TryGetValue(day, out var dayReminders);
            RenderDayCell(table.Cell(), day, monthStart, dayReminders ?? [], day == today);
        }
    }

    /// <summary>
    /// Renders one calendar day cell with a bounded number of reminder entries.
    /// </summary>
    /// <param name="container">The target day cell container.</param>
    /// <param name="day">The date represented by the cell.</param>
    /// <param name="monthStart">The first day of the selected month.</param>
    /// <param name="reminders">The reminders occurring on the date.</param>
    /// <param name="isToday">Whether the date is today.</param>
    private static void RenderDayCell(
        IContainer container,
        DateOnly day,
        DateOnly monthStart,
        IReadOnlyList<ReminderRowViewModel> reminders,
        bool isToday)
    {
        var isCurrentMonth = day.Year == monthStart.Year && day.Month == monthStart.Month;
        var background = !isCurrentMonth ? Adjacent : isToday ? Today : Colors.White;

        container.Height(64).Background(background).Border(0.5f).BorderColor(Grid).Padding(5).Column(column =>
        {
            column.Spacing(2);
            column.Item().Row(row =>
            {
                row.RelativeItem().Text(day.Day.ToString(CultureInfo.InvariantCulture)).FontSize(8).Bold().FontColor(isCurrentMonth ? Ink : "#9AA6B2");
                if (isToday)
                {
                    row.ConstantItem(34).AlignRight().Text("HOJE").FontSize(5.5f).Bold().FontColor(Accent);
                }
            });

            if (!isCurrentMonth)
            {
                return;
            }

            foreach (var reminder in reminders.Take(MaximumVisibleRemindersPerDay))
            {
                var status = GetStatusLabel(reminder);
                column.Item().Text($"{Truncate(reminder.Text, 25)} · {status}").FontSize(6.2f).FontColor(Ink);
            }

            var hiddenCount = reminders.Count - MaximumVisibleRemindersPerDay;
            if (hiddenCount > 0)
            {
                column.Item().Text($"+ {hiddenCount} adicionais").FontSize(6).SemiBold().FontColor(Muted);
            }
        });
    }

    /// <summary>
    /// Returns the Portuguese status label for a reminder.
    /// </summary>
    /// <param name="reminder">The reminder to classify.</param>
    /// <returns>The status label displayed in the PDF.</returns>
    private static string GetStatusLabel(ReminderRowViewModel reminder) =>
        reminder.IsAcknowledged ? "Confirmado" : reminder.IsAvailable ? "Por confirmar" : "Agendado";

    /// <summary>
    /// Truncates a text value to the requested maximum length.
    /// </summary>
    /// <param name="value">The text to truncate.</param>
    /// <param name="maximumLength">The maximum output length.</param>
    /// <returns>The original value or an ellipsis-terminated shortened value.</returns>
    private static string Truncate(string value, int maximumLength) =>
        value.Length <= maximumLength ? value : $"{value[..(maximumLength - 1)]}…";

    /// <summary>
    /// Renders the document footer.
    /// </summary>
    /// <param name="container">The target footer container.</param>
    private static void RenderFooter(IContainer container) => container.PaddingTop(8).Row(row =>
    {
        row.RelativeItem().Text($"Gerado em {DateTime.Now:dd/MM/yyyy HH:mm} - calendário do mês selecionado.").FontSize(6.5f).FontColor(Muted);
        row.ConstantItem(80).AlignRight().Text("DenariusAI").FontSize(6.5f).FontColor(Muted);
    });

    /// <summary>
    /// Uppercases the first character of a non-empty string.
    /// </summary>
    /// <param name="value">The text to transform.</param>
    /// <returns>The transformed string.</returns>
    private static string UpperFirst(string value) => string.IsNullOrEmpty(value) ? value : char.ToUpper(value[0]) + value[1..];
}
