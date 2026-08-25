namespace DenariusAI.Application.DTOs;

public sealed record ApplicationSettingsDto(
    string MistralModel,
    string MistralBaseUrl,
    int MistralMaxTokens,
    double MistralTemperature,
    string AssistantSystemPrompt,
    int AssistantContextMonths,
    int AssistantMaxTransactions,
    int AssistantHistoryMessages,
    string JournalSuggestionSystemPrompt,
    int JournalSuggestionHistoryMessages,
    string ReconciliationExtractionPrompt,
    string ReconciliationClassificationPrompt,
    string DashboardWelcomePrompt = Application.Configuration.ApplicationSettingsDefaults.DashboardWelcomePrompt);
