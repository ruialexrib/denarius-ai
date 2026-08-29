using DenariusAI.Domain.Common;

namespace DenariusAI.Domain.Entities;

public sealed class Warranty : AuditableEntity
{
    private Warranty() { }

    public Warranty(string name, string? supplier, DateOnly purchaseDate, DateOnly expiryDate, string? notes,
        string? documentFileName = null, string? documentBase64 = null) =>
        Update(name, supplier, purchaseDate, expiryDate, notes, documentFileName, documentBase64);

    public string Name { get; private set; } = string.Empty;
    public string? Supplier { get; private set; }
    public DateOnly PurchaseDate { get; private set; }
    public DateOnly ExpiryDate { get; private set; }
    public string? Notes { get; private set; }
    public string? DocumentFileName { get; private set; }
    public string DocumentContentType { get; private set; } = "application/pdf";
    public string? DocumentBase64 { get; private set; }
    public Reminder Reminder { get; private set; } = null!;

    public void Update(string name, string? supplier, DateOnly purchaseDate, DateOnly expiryDate, string? notes,
        string? documentFileName = null, string? documentBase64 = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("A designação é obrigatória.");
        if (expiryDate < purchaseDate) throw new ArgumentException("A data de fim da garantia não pode ser anterior à data de compra.");
        Name = name.Trim(); Supplier = Normalize(supplier); PurchaseDate = purchaseDate; ExpiryDate = expiryDate; Notes = Normalize(notes);
        if (documentFileName is not null && documentBase64 is not null)
        {
            DocumentFileName = documentFileName.Trim(); DocumentBase64 = documentBase64;
        }
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
