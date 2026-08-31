namespace DenariusAI.Application.DTOs;

/// <summary>
/// Represents the application settings configuration.
/// </summary>
/// <param name="MistralModel">The Mistral AI model identifier to use.</param>
/// <param name="MistralBaseUrl">The base URL for the Mistral AI API.</param>
/// <param name="MistralMaxTokens">The maximum number of tokens for Mistral AI responses.</param>
/// <param name="MistralTemperature">The temperature parameter for Mistral AI model responses, controlling randomness.</param>
/// <param name="AssistantSystemPrompt">The system prompt used for the assistant.</param>
/// <param name="AssistantContextMonths">The number of months to consider for assistant context.</param>
/// <param name="AssistantMaxTransactions">The maximum number of transactions to include in assistant context.</param>
/// <param name="AssistantHistoryMessages">The number of history messages to maintain for the assistant.</param>
/// <param name="JournalSuggestionSystemPrompt">The system prompt used for journal suggestions.</param>
/// <param name="JournalSuggestionHistoryMessages">The number of history messages to maintain for journal suggestions.</param>
/// <param name="ReconciliationExtractionPrompt">The prompt used for extracting data during reconciliation.</param>
/// <param name="ReconciliationClassificationPrompt">The prompt used for classifying transactions during reconciliation.</param>
/// <param name="DashboardWelcomePrompt">The welcome prompt displayed on the dashboard.</param>
/// <param name="FinancialAnalysisPrompt">The prompt used to generate the consolidated financial analysis.</param>
/// <param name="ConnectionTestPrompt">The prompt sent when testing the configured AI connection.</param>
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
    string DashboardWelcomePrompt = Application.Configuration.ApplicationSettingsDefaults.DashboardWelcomePrompt,
    string FinancialAnalysisPrompt = Application.Configuration.ApplicationSettingsDefaults.FinancialAnalysisPrompt,
    string ConnectionTestPrompt = Application.Configuration.ApplicationSettingsDefaults.ConnectionTestPrompt,
    string CorrespondenceMetadataPrompt = Application.Configuration.ApplicationSettingsDefaults.CorrespondenceMetadataPrompt,
    string MarketDataProvider = "AlphaVantage",
    string MarketDataBaseUrl = "https://www.alphavantage.co/query");
