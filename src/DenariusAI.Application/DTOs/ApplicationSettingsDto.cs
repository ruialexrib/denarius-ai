namespace DenariusAI.Application.DTOs;

/// <summary>Represents the application settings configuration.</summary>
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
    string SavingsCertificateClipboardPrompt = Application.Configuration.ApplicationSettingsDefaults.SavingsCertificateClipboardPrompt);
