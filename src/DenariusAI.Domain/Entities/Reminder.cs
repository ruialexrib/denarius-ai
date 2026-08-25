using DenariusAI.Domain.Common;

namespace DenariusAI.Domain.Entities;

public sealed class Reminder : AuditableEntity
{
    private Reminder() { }

    public Reminder(string text, DateOnly eventDate, int noticeDays) => Update(text, eventDate, noticeDays);

    public string Text { get; private set; } = string.Empty;
    public DateOnly EventDate { get; private set; }
    public int NoticeDays { get; private set; }
    public ICollection<ReminderAcknowledgement> Acknowledgements { get; } = [];

    public void Update(string text, DateOnly eventDate, int noticeDays)
    {
        if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("O texto é obrigatório.");
        if (noticeDays is < 0 or > 3650) throw new ArgumentOutOfRangeException(nameof(noticeDays));
        Text = text.Trim(); EventDate = eventDate; NoticeDays = noticeDays;
    }
}

public sealed class ReminderAcknowledgement
{
    public Guid ReminderId { get; set; }
    public Reminder Reminder { get; set; } = null!;
    public string UserId { get; set; } = string.Empty;
    public DateTimeOffset AcknowledgedAt { get; set; }
}
