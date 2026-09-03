namespace DenariusAI.Application.DTOs;

/// <summary>Represents the application settings configuration.</summary>
/// <param name="MistralModel">Mistral model identifier.</param>
/// <param name="MistralBaseUrl">Base URL of the Mistral API.</param>
/// <param name="MistralMaxTokens">Maximum number of generated tokens used by the configured AI provider.</param>
/// <param name="MistralTemperature">Generation temperature used by the configured AI provider.</param>
/// <param name="AssistantSystemPrompt">System prompt for the financial assistant.</param>
/// <param name="AssistantContextMonths">Number of months included in assistant context.</param>
/// <param name="AssistantMaxTransactions">Maximum number of transactions included in assistant context.</param>
/// <param name="AssistantHistoryMessages">Maximum number of assistant history messages.</param>
/// <param name="JournalSuggestionSystemPrompt">System prompt for journal-entry suggestions.</param>
/// <param name="JournalSuggestionHistoryMessages">Maximum number of journal suggestion history messages.</param>
/// <param name="ReconciliationExtractionPrompt">Prompt used to extract reconciliation movements.</param>
/// <param name="ReconciliationClassificationPrompt">Prompt used to classify reconciliation movements.</param>
/// <param name="DashboardWelcomePrompt">Prompt used for the dashboard welcome message.</param>
/// <param name="FinancialAnalysisPrompt">Prompt used for financial analysis.</param>
/// <param name="ConnectionTestPrompt">Prompt used to test the selected AI provider.</param>
/// <param name="CorrespondenceMetadataPrompt">Prompt used to extract correspondence metadata.</param>
/// <param name="MarketDataProvider">Configured market-data provider.</param>
/// <param name="MarketDataBaseUrl">Base URL of the market-data API.</param>
/// <param name="InsuranceClipboardPrompt">Prompt used to interpret insurance clipboard data.</param>
/// <param name="SavingsCertificateClipboardPrompt">Prompt used to interpret Savings Certificate clipboard data.</param>
/// <param name="AiProvider">Selected AI provider. Supported values are <c>Mistral</c> and <c>Ollama</c>.</param>
/// <param name="OllamaModel">Ollama model identifier sent to the chat API.</param>
/// <param name="OllamaBaseUrl">Base URL of the local or remote Ollama server.</param>
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
    string MarketDataBaseUrl = "https://www.alphavantage.co/query",
    string InsuranceClipboardPrompt = Application.Configuration.ApplicationSettingsDefaults.InsuranceClipboardPrompt,
    string SavingsCertificateClipboardPrompt = Application.Configuration.ApplicationSettingsDefaults.SavingsCertificateClipboardPrompt,
    string AiProvider = "Mistral",
    string OllamaModel = "llama3.2",
    string OllamaBaseUrl = "http://localhost:11434");
