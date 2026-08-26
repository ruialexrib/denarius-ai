using System.ComponentModel.DataAnnotations;

namespace DenariusAI.Web.ViewModels;

/// <summary>
/// Contains definitions for AssistantViewModels.
/// </summary>
public sealed class AssistantPageViewModel
{
    public bool IsAvailable { get; init; }
    public string Model { get; init; } = string.Empty;
}

public sealed class AssistantQuestionViewModel
{
    [Required, StringLength(1000)]
    public string Question { get; init; } = string.Empty;
    public IReadOnlyCollection<AssistantMessageViewModel> History { get; init; } = [];
}

public sealed class AssistantMessageViewModel
{
    public string Role { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
}
