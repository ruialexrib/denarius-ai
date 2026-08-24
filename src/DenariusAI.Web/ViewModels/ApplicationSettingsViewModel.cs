using System.ComponentModel.DataAnnotations;
using DenariusAI.Application.DTOs;

namespace DenariusAI.Web.ViewModels;

public sealed class ApplicationSettingsViewModel
{
    [Required, StringLength(100)] public string MistralModel { get; set; } = string.Empty;
    [Required, Url, StringLength(300)] public string MistralBaseUrl { get; set; } = string.Empty;
    [Range(64, 8192)] public int MistralMaxTokens { get; set; }
    [Range(0, 1)] public double MistralTemperature { get; set; }
    [Required, StringLength(10000)] public string AssistantSystemPrompt { get; set; } = string.Empty;
    [Range(1, 60)] public int AssistantContextMonths { get; set; }
    [Range(10, 1000)] public int AssistantMaxTransactions { get; set; }
    [Range(0, 50)] public int AssistantHistoryMessages { get; set; }
    [Required, StringLength(10000)] public string JournalSuggestionSystemPrompt { get; set; } = string.Empty;
    [Range(0, 50)] public int JournalSuggestionHistoryMessages { get; set; }
    public bool AiIsConfigured { get; set; }
    public ApplicationSettingsDto ToDto() => new(MistralModel, MistralBaseUrl, MistralMaxTokens, MistralTemperature, AssistantSystemPrompt, AssistantContextMonths, AssistantMaxTransactions, AssistantHistoryMessages, JournalSuggestionSystemPrompt, JournalSuggestionHistoryMessages);
    public static ApplicationSettingsViewModel From(ApplicationSettingsDto value, bool configured) => new() { MistralModel = value.MistralModel, MistralBaseUrl = value.MistralBaseUrl, MistralMaxTokens = value.MistralMaxTokens, MistralTemperature = value.MistralTemperature, AssistantSystemPrompt = value.AssistantSystemPrompt, AssistantContextMonths = value.AssistantContextMonths, AssistantMaxTransactions = value.AssistantMaxTransactions, AssistantHistoryMessages = value.AssistantHistoryMessages, JournalSuggestionSystemPrompt = value.JournalSuggestionSystemPrompt, JournalSuggestionHistoryMessages = value.JournalSuggestionHistoryMessages, AiIsConfigured = configured };
}
