using System.Globalization;
using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Application.Configuration;
using DenariusAI.Application.DTOs;
using DenariusAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DenariusAI.Infrastructure.ArtificialIntelligence;

/// <summary>Reads and updates effective application settings with legacy-key compatibility.</summary>
/// <param name="dbContext">The application settings store.</param>
/// <param name="mistralOptions">Installation defaults retained for existing deployments.</param>
public sealed class ApplicationSettingsService(DenariusDbContext dbContext, IOptions<MistralOptions> mistralOptions) : IApplicationSettingsService
{
    /// <summary>Loads effective settings, preferring provider-neutral generation keys.</summary>
    /// <param name="cancellationToken">Token used to cancel the database operation.</param>
    /// <returns>Effective settings including administrator-configured prompts.</returns>
    public async Task<ApplicationSettingsDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var values = await dbContext.ApplicationSettings.AsNoTracking().ToDictionaryAsync(item => item.Key, item => item.Value, cancellationToken);
        var defaults = mistralOptions.Value;
        return new(
            Get(values, "Mistral.Model", defaults.Model), Get(values, "Mistral.BaseUrl", defaults.BaseUrl), GetInt(values, "AI.MaxTokens", GetInt(values, "Mistral.MaxTokens", defaults.MaxTokens)), GetDouble(values, "AI.Temperature", GetDouble(values, "Mistral.Temperature", defaults.Temperature)),
            Get(values, "Prompts.Assistant", ApplicationSettingsDefaults.AssistantPrompt), GetInt(values, "Assistant.ContextMonths", 12), GetInt(values, "Assistant.MaxTransactions", 200), GetInt(values, "Assistant.HistoryMessages", 10),
            UpgradeDefault(Get(values, "Prompts.JournalSuggestion", ApplicationSettingsDefaults.JournalSuggestionPrompt), ApplicationSettingsDefaults.LegacyJournalSuggestionPrompt, ApplicationSettingsDefaults.JournalSuggestionPrompt), GetInt(values, "JournalSuggestion.HistoryMessages", 10),
            UpgradeDefault(Get(values, "Prompts.ReconciliationExtraction", ApplicationSettingsDefaults.ReconciliationExtractionPrompt), ApplicationSettingsDefaults.LegacyReconciliationExtractionPrompt, ApplicationSettingsDefaults.ReconciliationExtractionPrompt),
            UpgradeDefault(Get(values, "Prompts.ReconciliationClassification", ApplicationSettingsDefaults.ReconciliationClassificationPrompt), ApplicationSettingsDefaults.LegacyReconciliationClassificationPrompt, ApplicationSettingsDefaults.ReconciliationClassificationPrompt),
            UpgradeDefault(Get(values, "Prompts.DashboardWelcome", ApplicationSettingsDefaults.DashboardWelcomePrompt), ApplicationSettingsDefaults.LegacyDashboardWelcomePrompt, ApplicationSettingsDefaults.DashboardWelcomePrompt),
            Get(values, "Prompts.FinancialAnalysis", ApplicationSettingsDefaults.FinancialAnalysisPrompt), Get(values, "Prompts.ConnectionTest", ApplicationSettingsDefaults.ConnectionTestPrompt), Get(values, "Prompts.CorrespondenceMetadata", ApplicationSettingsDefaults.CorrespondenceMetadataPrompt),
            Get(values, "MarketData.Provider", "AlphaVantage"), Get(values, "MarketData.BaseUrl", "https://www.alphavantage.co/query"),
            UpgradeDefault(Get(values, "Prompts.InsuranceClipboard", ApplicationSettingsDefaults.InsuranceClipboardPrompt), ApplicationSettingsDefaults.LegacyInsuranceClipboardPrompt, ApplicationSettingsDefaults.InsuranceClipboardPrompt),
            Get(values, "Prompts.SavingsCertificateClipboard", ApplicationSettingsDefaults.SavingsCertificateClipboardPrompt),
            Get(values, "AI.Provider", "Mistral"), Get(values, "Ollama.Model", "llama3.2"), Get(values, "Ollama.BaseUrl", "http://localhost:11434"));
    }

    /// <summary>Validates and persists settings without changing provider credentials.</summary>
    /// <param name="settings">The settings to apply.</param>
    /// <param name="userId">The administrator responsible for the update.</param>
    /// <param name="cancellationToken">Token used to cancel the database operation.</param>
    /// <returns>A task completing after settings are saved.</returns>
    /// <exception cref="ArgumentException">A setting is outside its supported range.</exception>
    public async Task UpdateAsync(ApplicationSettingsDto settings, string userId, CancellationToken cancellationToken = default)
    {
        Validate(settings);
        var values = new Dictionary<string, string>
        {
            ["AI.Provider"] = settings.AiProvider.Trim(), ["Ollama.Model"] = settings.OllamaModel.Trim(), ["Ollama.BaseUrl"] = settings.OllamaBaseUrl.Trim(),
            ["Mistral.Model"] = settings.MistralModel.Trim(), ["Mistral.BaseUrl"] = settings.MistralBaseUrl.Trim(), ["AI.MaxTokens"] = settings.AiMaxTokens.ToString(CultureInfo.InvariantCulture), ["AI.Temperature"] = settings.AiTemperature.ToString(CultureInfo.InvariantCulture),
            ["Prompts.Assistant"] = settings.AssistantSystemPrompt.Trim(), ["Assistant.ContextMonths"] = settings.AssistantContextMonths.ToString(CultureInfo.InvariantCulture), ["Assistant.MaxTransactions"] = settings.AssistantMaxTransactions.ToString(CultureInfo.InvariantCulture), ["Assistant.HistoryMessages"] = settings.AssistantHistoryMessages.ToString(CultureInfo.InvariantCulture),
            ["Prompts.JournalSuggestion"] = settings.JournalSuggestionSystemPrompt.Trim(), ["JournalSuggestion.HistoryMessages"] = settings.JournalSuggestionHistoryMessages.ToString(CultureInfo.InvariantCulture), ["Prompts.ReconciliationExtraction"] = settings.ReconciliationExtractionPrompt.Trim(), ["Prompts.ReconciliationClassification"] = settings.ReconciliationClassificationPrompt.Trim(),
            ["Prompts.DashboardWelcome"] = settings.DashboardWelcomePrompt.Trim(), ["Prompts.FinancialAnalysis"] = settings.FinancialAnalysisPrompt.Trim(), ["Prompts.ConnectionTest"] = settings.ConnectionTestPrompt.Trim(), ["Prompts.CorrespondenceMetadata"] = settings.CorrespondenceMetadataPrompt.Trim(),
            ["MarketData.Provider"] = settings.MarketDataProvider.Trim(), ["MarketData.BaseUrl"] = settings.MarketDataBaseUrl.Trim(), ["Prompts.InsuranceClipboard"] = settings.InsuranceClipboardPrompt.Trim(), ["Prompts.SavingsCertificateClipboard"] = settings.SavingsCertificateClipboardPrompt.Trim()
        };
        var existing = await dbContext.ApplicationSettings.ToDictionaryAsync(item => item.Key, cancellationToken);
        foreach (var pair in values) { if (existing.TryGetValue(pair.Key, out var setting)) { setting.Value = pair.Value; setting.UpdatedBy = userId; } else dbContext.ApplicationSettings.Add(new ApplicationSetting { Key = pair.Key, Value = pair.Value, CreatedBy = userId }); }
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Checks provider settings, prompts and application limits before persistence.</summary>
    /// <param name="value">The proposed settings.</param>
    /// <exception cref="ArgumentException">A setting is invalid.</exception>
    private static void Validate(ApplicationSettingsDto value)
    {
        if (!string.Equals(value.AiProvider, "Mistral", StringComparison.OrdinalIgnoreCase) && !string.Equals(value.AiProvider, "Ollama", StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("O fornecedor de IA deve ser Mistral ou Ollama.");
        if (string.IsNullOrWhiteSpace(value.MistralModel) || string.IsNullOrWhiteSpace(value.OllamaModel)) throw new ArgumentException("Os modelos de IA são obrigatórios.");
        if (!Uri.TryCreate(value.MistralBaseUrl, UriKind.Absolute, out var mistralUri) || mistralUri.Scheme != Uri.UriSchemeHttps) throw new ArgumentException("O endereço da Mistral deve ser um URL HTTPS válido.");
        if (!Uri.TryCreate(value.OllamaBaseUrl, UriKind.Absolute, out var ollamaUri) || (ollamaUri.Scheme != Uri.UriSchemeHttp && ollamaUri.Scheme != Uri.UriSchemeHttps)) throw new ArgumentException("O endereço do Ollama deve ser um URL HTTP ou HTTPS válido.");
        if (string.IsNullOrWhiteSpace(value.AssistantSystemPrompt) || string.IsNullOrWhiteSpace(value.JournalSuggestionSystemPrompt) || string.IsNullOrWhiteSpace(value.ReconciliationExtractionPrompt) || string.IsNullOrWhiteSpace(value.ReconciliationClassificationPrompt) || string.IsNullOrWhiteSpace(value.DashboardWelcomePrompt) || string.IsNullOrWhiteSpace(value.FinancialAnalysisPrompt) || string.IsNullOrWhiteSpace(value.ConnectionTestPrompt) || string.IsNullOrWhiteSpace(value.CorrespondenceMetadataPrompt) || string.IsNullOrWhiteSpace(value.InsuranceClipboardPrompt) || string.IsNullOrWhiteSpace(value.SavingsCertificateClipboardPrompt)) throw new ArgumentException("Os prompts são obrigatórios.");
        if (!string.Equals(value.MarketDataProvider, "AlphaVantage", StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("O fornecedor gratuito suportado é Alpha Vantage.");
        if (!Uri.TryCreate(value.MarketDataBaseUrl, UriKind.Absolute, out var marketUri) || marketUri.Scheme != Uri.UriSchemeHttps) throw new ArgumentException("O endereço do fornecedor de cotações deve ser um URL HTTPS válido.");
        if (value.AiMaxTokens is < 64 or > 8192 || value.AiTemperature is < 0 or > 1) throw new ArgumentException("Os parâmetros do modelo estão fora dos limites permitidos.");
        if (value.AssistantContextMonths is < 1 or > 60 || value.AssistantMaxTransactions is < 10 or > 1000 || value.AssistantHistoryMessages is < 0 or > 50 || value.JournalSuggestionHistoryMessages is < 0 or > 50) throw new ArgumentException("Os limites da aplicação estão fora dos intervalos permitidos.");
    }

    /// <summary>Reads a setting with an installation fallback.</summary>
    /// <param name="values">Stored application settings.</param>
    /// <param name="key">The setting key.</param>
    /// <param name="fallback">The value used when missing or invalid.</param>
    /// <returns>The stored or fallback value.</returns>
    private static string Get(IReadOnlyDictionary<string, string> values, string key, string fallback) => values.GetValueOrDefault(key, fallback);
    /// <summary>Updates an unchanged legacy prompt while preserving custom prompts.</summary>
    /// <param name="value">The stored prompt.</param>
    /// <param name="legacyDefault">The previous default prompt.</param>
    /// <param name="currentDefault">The current default prompt.</param>
    /// <returns>The effective prompt.</returns>
    private static string UpgradeDefault(string value, string legacyDefault, string currentDefault) => string.Equals(value.Trim(), legacyDefault, StringComparison.Ordinal) ? currentDefault : value;
    /// <summary>Parses an invariant integer setting.</summary>
    /// <param name="values">Stored application settings.</param>
    /// <param name="key">The setting key.</param>
    /// <param name="fallback">The value used when missing or invalid.</param>
    /// <returns>The parsed value or fallback.</returns>
    private static int GetInt(IReadOnlyDictionary<string, string> values, string key, int fallback) => int.TryParse(values.GetValueOrDefault(key), CultureInfo.InvariantCulture, out var value) ? value : fallback;
    /// <summary>Parses an invariant floating-point setting.</summary>
    /// <param name="values">Stored application settings.</param>
    /// <param name="key">The setting key.</param>
    /// <param name="fallback">The value used when missing or invalid.</param>
    /// <returns>The parsed value or fallback.</returns>
    private static double GetDouble(IReadOnlyDictionary<string, string> values, string key, double fallback) => double.TryParse(values.GetValueOrDefault(key), CultureInfo.InvariantCulture, out var value) ? value : fallback;
}
