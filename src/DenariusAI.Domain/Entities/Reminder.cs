using DenariusAI.Domain.Common;

namespace DenariusAI.Domain.Entities;

/// <summary>
/// Represents a reminder entity that tracks events and notifications.
/// </summary>
public sealed class Reminder : AuditableEntity
{
    /// <summary>
    /// Initializes an empty reminder instance for persistence materialization.
    /// </summary>
    private Reminder() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Reminder"/> class.
    /// </summary>
    /// <param name="text">The reminder text.</param>
    /// <param name="eventDate">The date of the event.</param>
    /// <param name="noticeDays">The number of days before the event to send notifications.</param>
    public Reminder(string text, DateOnly eventDate, int noticeDays) => Update(text, eventDate, noticeDays);

    /// <summary>
    /// Gets the reminder text.
    /// </summary>
    public string Text { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the date when the event occurs.
    /// </summary>
    public DateOnly EventDate { get; private set; }

    /// <summary>
    /// Gets the number of days before the event to send notifications.
    /// </summary>
    public int NoticeDays { get; private set; }

    /// <summary>Gets the identifier of the linked warranty, when applicable.</summary>
    public Guid? WarrantyId { get; private set; }

    /// <summary>Gets the linked warranty, when this reminder belongs to a warranty.</summary>
    public Warranty? Warranty { get; private set; }

    /// <summary>Gets the identifier of the linked savings certificate, when applicable.</summary>
    public Guid? SavingsCertificateId { get; private set; }

    /// <summary>Gets the linked savings certificate, when this reminder belongs to a certificate.</summary>
    public SavingsCertificate? SavingsCertificate { get; private set; }

    /// <summary>
    /// Gets the collection of acknowledgements for this reminder.
    /// </summary>
    public ICollection<ReminderAcknowledgement> Acknowledgements { get; } = [];

    /// <summary>
    /// Updates the reminder properties.
    /// </summary>
    /// <param name="text">The reminder text.</param>
    /// <param name="eventDate">The date of the event.</param>
    /// <param name="noticeDays">The number of days before the event to send notifications.</param>
    /// <exception cref="ArgumentException">Thrown when the text is null or whitespace.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when notice days is outside the valid range (0-3650).</exception>
    public void Update(string text, DateOnly eventDate, int noticeDays)
    {
        if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("O texto é obrigatório.");
        if (noticeDays is < 0 or > 3650) throw new ArgumentOutOfRangeException(nameof(noticeDays));
        Text = text.Trim(); EventDate = eventDate; NoticeDays = noticeDays;
    }

    /// <summary>Links this reminder to a warranty.</summary>
    /// <param name="warrantyId">The warranty identifier.</param>
    public void LinkToWarranty(Guid warrantyId) => WarrantyId = warrantyId;

    /// <summary>Links this reminder to a savings certificate.</summary>
    /// <param name="savingsCertificateId">The savings certificate identifier.</param>
    public void LinkToSavingsCertificate(Guid savingsCertificateId) => SavingsCertificateId = savingsCertificateId;
}

/// <summary>
/// Represents a user acknowledgement of a reminder notification.
/// </summary>
public sealed class ReminderAcknowledgement
{
    /// <summary>
    /// Gets or sets the reminder identifier.
    /// </summary>
    public Guid ReminderId { get; set; }

    /// <summary>
    /// Gets or sets the associated reminder.
    /// </summary>
    public Reminder Reminder { get; set; } = null!;

    /// <summary>
    /// Gets or sets the user identifier who acknowledged the reminder.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when the reminder was acknowledged.
    /// </summary>
    public DateTimeOffset AcknowledgedAt { get; set; }
}
