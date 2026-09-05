using DenariusAI.Domain.Common;

namespace DenariusAI.Domain.Entities;

/// <summary>
/// Represents a purchased item warranty and its optional supporting PDF document.
/// </summary>
public sealed class Warranty : AuditableEntity
{
    /// <summary>
    /// Initializes an empty warranty instance for persistence materialization.
    /// </summary>
    private Warranty() { }

    /// <summary>
    /// Initializes a warranty with its purchase, expiry and optional document details.
    /// </summary>
    /// <param name="name">The warranty or covered item name.</param>
    /// <param name="supplier">The optional supplier or retailer.</param>
    /// <param name="purchaseDate">The purchase date.</param>
    /// <param name="expiryDate">The warranty expiry date.</param>
    /// <param name="notes">Optional notes about the warranty.</param>
    /// <param name="documentFileName">The optional PDF document file name.</param>
    /// <param name="documentBase64">The optional PDF document encoded as Base64.</param>
    /// <exception cref="ArgumentException">Thrown when the name is missing or the expiry date precedes the purchase date.</exception>
    public Warranty(string name, string? supplier, DateOnly purchaseDate, DateOnly expiryDate, string? notes,
        string? documentFileName = null, string? documentBase64 = null) =>
        Update(name, supplier, purchaseDate, expiryDate, notes, documentFileName, documentBase64);

    /// <summary>Gets the warranty or covered item name.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the optional supplier or retailer.</summary>
    public string? Supplier { get; private set; }

    /// <summary>Gets the purchase date.</summary>
    public DateOnly PurchaseDate { get; private set; }

    /// <summary>Gets the warranty expiry date.</summary>
    public DateOnly ExpiryDate { get; private set; }

    /// <summary>Gets optional notes about the warranty.</summary>
    public string? Notes { get; private set; }

    /// <summary>Gets the stored PDF document file name, when available.</summary>
    public string? DocumentFileName { get; private set; }

    /// <summary>Gets the MIME type used for the stored warranty document.</summary>
    public string DocumentContentType { get; private set; } = "application/pdf";

    /// <summary>Gets the stored PDF document encoded as Base64, when available.</summary>
    public string? DocumentBase64 { get; private set; }

    /// <summary>Gets the reminder associated with the warranty expiry.</summary>
    public Reminder Reminder { get; private set; } = null!;

    /// <summary>
    /// Updates the warranty details while preserving an existing document unless a complete replacement is supplied.
    /// </summary>
    /// <param name="name">The warranty or covered item name.</param>
    /// <param name="supplier">The optional supplier or retailer.</param>
    /// <param name="purchaseDate">The purchase date.</param>
    /// <param name="expiryDate">The warranty expiry date.</param>
    /// <param name="notes">Optional notes about the warranty.</param>
    /// <param name="documentFileName">The optional replacement PDF document file name.</param>
    /// <param name="documentBase64">The optional replacement PDF document encoded as Base64.</param>
    /// <exception cref="ArgumentException">Thrown when the name is missing or the expiry date precedes the purchase date.</exception>
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

    /// <summary>
    /// Trims an optional text value and converts blank input to <see langword="null"/>.
    /// </summary>
    /// <param name="value">The optional text value to normalize.</param>
    /// <returns>The trimmed value, or <see langword="null"/> when the input is blank.</returns>
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
