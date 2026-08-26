using System.ComponentModel.DataAnnotations;

namespace DenariusAI.Web.ViewModels;

/// <summary>
/// Represents the ReminderViewModels type.
/// </summary>
public sealed class ReminderFormViewModel
{
    public Guid Id { get; set; }
    [Required, StringLength(500), Display(Name = "Texto do lembrete")]
    public string Text { get; set; } = string.Empty;
    [Required, DataType(DataType.Date), Display(Name = "Data do evento")]
    public DateOnly EventDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(7));
    [Range(0, 3650), Display(Name = "Avisar com quantos dias de antecedência")]
    public int NoticeDays { get; set; } = 3;
}

/// <summary>
/// Represents the ReminderRowViewModel type.
/// </summary>
public sealed record ReminderRowViewModel(Guid Id, string Text, DateOnly EventDate, int NoticeDays, bool IsAvailable, bool IsAcknowledged, int DaysRemaining);
/// <summary>
/// Represents the ReminderIndexViewModel type.
/// </summary>
public sealed record ReminderIndexViewModel(
    IReadOnlyList<ReminderRowViewModel> Items,
    int ActiveCount,
    int ScheduledCount,
    int AcknowledgedCount,
    int TotalCount,
    string? Search,
    string Status,
    DateOnly? From,
    DateOnly? To);
/// <summary>
/// Represents the DashboardReminderViewModel type.
/// </summary>
public sealed record DashboardReminderViewModel(Guid Id, string Text, DateOnly EventDate, int DaysRemaining);
