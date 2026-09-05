namespace DenariusAI.Web.Models;

/// <summary>
/// Supplies diagnostic request information to the error view without exposing application internals.
/// </summary>
public sealed class ErrorViewModel
{
    /// <summary>Gets the request identifier associated with the error, when available.</summary>
    public string? RequestId { get; init; }

    /// <summary>Gets a value indicating whether the request identifier should be displayed.</summary>
    public bool ShowRequestId => !string.IsNullOrWhiteSpace(RequestId);
}
