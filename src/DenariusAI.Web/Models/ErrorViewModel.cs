namespace DenariusAI.Web.Models;

/// <summary>
/// Contains definitions for ErrorViewModel.
/// </summary>
public sealed class ErrorViewModel
{
    public string? RequestId { get; init; }
    public bool ShowRequestId => !string.IsNullOrWhiteSpace(RequestId);
}
