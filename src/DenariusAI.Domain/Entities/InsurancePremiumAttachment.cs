using DenariusAI.Domain.Common;

namespace DenariusAI.Domain.Entities;

/// <summary>
/// Represents a supporting PDF document attached to an insurance premium.
/// </summary>
public sealed class InsurancePremiumAttachment : AuditableEntity
{
    /// <summary>Initializes an empty attachment for Entity Framework Core.</summary>
    private InsurancePremiumAttachment() { }

    /// <summary>Creates a premium attachment.</summary>
    /// <param name="premiumId">Owning premium identifier.</param>
    /// <param name="fileName">Original file name.</param>
    /// <param name="contentType">MIME content type.</param>
    /// <param name="documentBase64">Base64 encoded PDF content.</param>
    /// <exception cref="ArgumentException">Thrown when attachment data is invalid.</exception>
    public InsurancePremiumAttachment(Guid premiumId, string fileName, string contentType, string documentBase64)
    {
        if (premiumId == Guid.Empty) throw new ArgumentException("Premium is required.", nameof(premiumId));
        if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("File name is required.", nameof(fileName));
        if (!string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Only PDF attachments are supported.", nameof(contentType));
        if (string.IsNullOrWhiteSpace(documentBase64)) throw new ArgumentException("Document content is required.", nameof(documentBase64));
        PremiumId = premiumId; FileName = fileName.Trim(); ContentType = "application/pdf"; DocumentBase64 = documentBase64;
    }

    /// <summary>Gets the owning premium identifier.</summary>
    public Guid PremiumId { get; private set; }
    /// <summary>Gets the owning premium.</summary>
    public InsurancePremium Premium { get; private set; } = null!;
    /// <summary>Gets the original file name.</summary>
    public string FileName { get; private set; } = string.Empty;
    /// <summary>Gets the MIME content type.</summary>
    public string ContentType { get; private set; } = "application/pdf";
    /// <summary>Gets the Base64 encoded PDF content.</summary>
    public string DocumentBase64 { get; private set; } = string.Empty;
}
