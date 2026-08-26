namespace DenariusAI.Web.Models;

/// <summary>
/// Represents the ErrorViewModel type.
/// </summary>
public sealed class ErrorViewModel
{
    public string? RequestId { get; init; }
    public bool ShowRequestId => !string.IsNullOrWhiteSpace(RequestId);
}
