using DenariusAI.Domain.Common;

namespace DenariusAI.Domain.Entities;

/// <summary>Represents a general PDF document attached directly to an insurance policy.</summary>
public sealed class InsurancePolicyAttachment : AuditableEntity
{
    /// <summary>Initializes an empty attachment for Entity Framework Core.</summary>
    private InsurancePolicyAttachment() { }

    /// <summary>Creates a general policy attachment.</summary>
    /// <param name="policyId">Owning policy identifier.</param>
    /// <param name="fileName">Original file name.</param>
    /// <param name="contentType">MIME content type.</param>
    /// <param name="documentBase64">Base64 encoded PDF content.</param>
    /// <exception cref="ArgumentException">Thrown when attachment data is invalid.</exception>
    public InsurancePolicyAttachment(Guid policyId, string fileName, string contentType, string documentBase64)
    {
        if (policyId == Guid.Empty) throw new ArgumentException("Policy is required.", nameof(policyId));
        if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("File name is required.", nameof(fileName));
        if (!string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Only PDF attachments are supported.", nameof(contentType));
        if (string.IsNullOrWhiteSpace(documentBase64)) throw new ArgumentException("Document content is required.", nameof(documentBase64));
        PolicyId = policyId;
        FileName = fileName.Trim();
        ContentType = "application/pdf";
        DocumentBase64 = documentBase64;
    }

    /// <summary>Gets the owning policy identifier.</summary>
    public Guid PolicyId { get; private set; }
    /// <summary>Gets the owning policy.</summary>
    public InsurancePolicy Policy { get; private set; } = null!;
    /// <summary>Gets the original file name.</summary>
    public string FileName { get; private set; } = string.Empty;
    /// <summary>Gets the MIME content type.</summary>
    public string ContentType { get; private set; } = "application/pdf";
    /// <summary>Gets the Base64 encoded PDF content.</summary>
    public string DocumentBase64 { get; private set; } = string.Empty;
}
