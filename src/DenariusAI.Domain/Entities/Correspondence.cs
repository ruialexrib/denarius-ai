using DenariusAI.Domain.Common;

namespace DenariusAI.Domain.Entities;

public sealed class Correspondence : AuditableEntity
{
    private Correspondence() { }

    public Correspondence(string subject, string? sender, DateOnly receivedDate, string? notes,
        string? documentFileName = null, string? documentBase64 = null) =>
        Update(subject, sender, receivedDate, notes, documentFileName, documentBase64);

    public string Subject { get; private set; } = string.Empty;
    public string? Sender { get; private set; }
    public DateOnly ReceivedDate { get; private set; }
    public string? Notes { get; private set; }
    public string? DocumentFileName { get; private set; }
    public string DocumentContentType { get; private set; } = "application/pdf";
    public string? DocumentBase64 { get; private set; }
    public ICollection<CorrespondenceMetadata> Metadata { get; } = [];

    public void Update(string subject, string? sender, DateOnly receivedDate, string? notes,
        string? documentFileName = null, string? documentBase64 = null)
    {
        if (string.IsNullOrWhiteSpace(subject)) throw new ArgumentException("O assunto é obrigatório.");
        Subject = subject.Trim(); Sender = Normalize(sender); ReceivedDate = receivedDate; Notes = Normalize(notes);
        if (documentFileName is not null && documentBase64 is not null)
        {
            DocumentFileName = documentFileName.Trim(); DocumentBase64 = documentBase64;
        }
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class CorrespondenceMetadata : AuditableEntity
{
    private CorrespondenceMetadata() { }

    public CorrespondenceMetadata(Guid correspondenceId, string key, string value, string? confidence) =>
        Update(correspondenceId, key, value, confidence);

    public Guid CorrespondenceId { get; private set; }
    public Correspondence Correspondence { get; private set; } = null!;
    public string Key { get; private set; } = string.Empty;
    public string Value { get; private set; } = string.Empty;
    public string? Confidence { get; private set; }

    public void Update(Guid correspondenceId, string key, string value, string? confidence)
    {
        if (correspondenceId == Guid.Empty) throw new ArgumentException("A correspondência é obrigatória.");
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("A chave é obrigatória.");
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("O valor é obrigatório.");
        var normalizedConfidence = confidence?.Trim().ToLowerInvariant();
        if (normalizedConfidence is not null and not "high" and not "low") throw new ArgumentException("A confiança indicada não é válida.");
        CorrespondenceId = correspondenceId; Key = key.Trim(); Value = value.Trim(); Confidence = normalizedConfidence;
    }
}
