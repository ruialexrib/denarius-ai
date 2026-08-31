using System.Globalization;
using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Application.Configuration;
using DenariusAI.Application.DTOs;
using DenariusAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DenariusAI.Infrastructure.ArtificialIntelligence;

/// <summary>
/// Service for managing application settings stored in the database.
/// </summary>
/// <param name="dbContext">The database context for accessing application settings.</param>
/// <param name="mistralOptions">Configuration options for Mistral AI integration.</param>
public sealed class ApplicationSettingsService(DenariusDbContext dbContext, IOptions<MistralOptions> mistralOptions) : IApplicationSettingsService
{
    /// <summary>
    /// Retrieves the current application settings.
    /// </summary>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation, containing the application settings.</returns>
    public async Task<ApplicationSettingsDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var values = await dbContext.ApplicationSettings.AsNoTracking().ToDictionaryAsync(item => item.Key, item => item.Value, cancellationToken);
        var defaults = mistralOptions.Value;
        return new(
            Get(values, "Mistral.Model", defaults.Model),
            Get(values, "Mistral.BaseUrl", defaults.BaseUrl),
            GetInt(values, "Mistral.MaxTokens", defaults.MaxTokens),
            GetDouble(values, "Mistral.Temperature", defaults.Temperature),
            Get(values, "Prompts.Assistant", ApplicationSettingsDefaults.AssistantPrompt),
            GetInt(values, "Assistant.ContextMonths", 12),
            GetInt(values, "Assistant.MaxTransactions", 200),
            GetInt(values, "Assistant.HistoryMessages", 10),
            UpgradeDefault(Get(values, "Prompts.JournalSuggestion", ApplicationSettingsDefaults.JournalSuggestionPrompt), ApplicationSettingsDefaults.LegacyJournalSuggestionPrompt, ApplicationSettingsDefaults.JournalSuggestionPrompt),
            GetInt(values, "JournalSuggestion.HistoryMessages", 10),
            UpgradeDefault(Get(values, "Prompts.ReconciliationExtraction", ApplicationSettingsDefaults.ReconciliationExtractionPrompt), ApplicationSettingsDefaults.LegacyReconciliationExtractionPrompt, ApplicationSettingsDefaults.ReconciliationExtractionPrompt),
            UpgradeDefault(Get(values, "Prompts.ReconciliationClassification", ApplicationSettingsDefaults.ReconciliationClassificationPrompt), ApplicationSettingsDefaults.LegacyReconciliationClassificationPrompt, ApplicationSettingsDefaults.ReconciliationClassificationPrompt),
            UpgradeDefault(Get(values, "Prompts.DashboardWelcome", ApplicationSettingsDefaults.DashboardWelcomePrompt), ApplicationSettingsDefaults.LegacyDashboardWelcomePrompt, ApplicationSettingsDefaults.DashboardWelcomePrompt),
            Get(values, "Prompts.FinancialAnalysis", ApplicationSettingsDefaults.FinancialAnalysisPrompt),
            Get(values, "Prompts.ConnectionTest", ApplicationSettingsDefaults.ConnectionTestPrompt),
            Get(values, "Prompts.CorrespondenceMetadata", ApplicationSettingsDefaults.CorrespondenceMetadataPrompt),
            Get(values, "MarketData.Provider", "AlphaVantage"),
            Get(values, "MarketData.BaseUrl", "https://www.alphavantage.co/query"));
    }

    /// <summary>
    /// Updates the application settings in the database.
    /// </summary>
    /// <param name="settings">The new settings to be saved.</param>
    /// <param name="userId">The ID of the user performing the update.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task UpdateAsync(ApplicationSettingsDto settings, string userId, CancellationToken cancellationToken = default)
    {
        Validate(settings);
        var values = new Dictionary<string, string>
        {
            ["Mistral.Model"] = settings.MistralModel.Trim(), ["Mistral.BaseUrl"] = settings.MistralBaseUrl.Trim(),
            ["Mistral.MaxTokens"] = settings.MistralMaxTokens.ToString(CultureInfo.InvariantCulture), ["Mistral.Temperature"] = settings.MistralTemperature.ToString(CultureInfo.InvariantCulture),
            ["Prompts.Assistant"] = settings.AssistantSystemPrompt.Trim(), ["Assistant.ContextMonths"] = settings.AssistantContextMonths.ToString(CultureInfo.InvariantCulture),
            ["Assistant.MaxTransactions"] = settings.AssistantMaxTransactions.ToString(CultureInfo.InvariantCulture), ["Assistant.HistoryMessages"] = settings.AssistantHistoryMessages.ToString(CultureInfo.InvariantCulture),
            ["Prompts.JournalSuggestion"] = settings.JournalSuggestionSystemPrompt.Trim(), ["JournalSuggestion.HistoryMessages"] = settings.JournalSuggestionHistoryMessages.ToString(CultureInfo.InvariantCulture),
            ["Prompts.ReconciliationExtraction"] = settings.ReconciliationExtractionPrompt.Trim(), ["Prompts.ReconciliationClassification"] = settings.ReconciliationClassificationPrompt.Trim(),
            ["Prompts.DashboardWelcome"] = settings.DashboardWelcomePrompt.Trim(),
            ["Prompts.FinancialAnalysis"] = settings.FinancialAnalysisPrompt.Trim(),
            ["Prompts.ConnectionTest"] = settings.ConnectionTestPrompt.Trim(),
            ["Prompts.CorrespondenceMetadata"] = settings.CorrespondenceMetadataPrompt.Trim(),
            ["MarketData.Provider"] = settings.MarketDataProvider.Trim(),
            ["MarketData.BaseUrl"] = settings.MarketDataBaseUrl.Trim()
        };
        var existing = await dbContext.ApplicationSettings.ToDictionaryAsync(item => item.Key, cancellationToken);
        foreach (var pair in values)
        {
            if (existing.TryGetValue(pair.Key, out var setting)) { setting.Value = pair.Value; setting.UpdatedBy = userId; }
            else dbContext.ApplicationSettings.Add(new ApplicationSetting { Key = pair.Key, Value = pair.Value, CreatedBy = userId });
        }
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Validates the application settings to ensure all required fields are present and within valid ranges.
    /// </summary>
    /// <param name="value">The settings to validate.</param>
    /// <exception cref="ArgumentException">Thrown when validation fails.</exception>
    private static void Validate(ApplicationSettingsDto value)
    {
        if (string.IsNullOrWhiteSpace(value.MistralModel) || string.IsNullOrWhiteSpace(value.AssistantSystemPrompt) || string.IsNullOrWhiteSpace(value.JournalSuggestionSystemPrompt) || string.IsNullOrWhiteSpace(value.ReconciliationExtractionPrompt) || string.IsNullOrWhiteSpace(value.ReconciliationClassificationPrompt) || string.IsNullOrWhiteSpace(value.DashboardWelcomePrompt) || string.IsNullOrWhiteSpace(value.FinancialAnalysisPrompt) || string.IsNullOrWhiteSpace(value.ConnectionTestPrompt) || string.IsNullOrWhiteSpace(value.CorrespondenceMetadataPrompt)) throw new ArgumentException("Modelo e prompts são obrigatórios.");
        if (!Uri.TryCreate(value.MistralBaseUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps) throw new ArgumentException("O endereço da Mistral deve ser um URL HTTPS válido.");
        if (!string.Equals(value.MarketDataProvider, "AlphaVantage", StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("O fornecedor gratuito suportado é Alpha Vantage.");
        if (!Uri.TryCreate(value.MarketDataBaseUrl, UriKind.Absolute, out var marketUri) || marketUri.Scheme != Uri.UriSchemeHttps) throw new ArgumentException("O endereço do fornecedor de cotações deve ser um URL HTTPS válido.");
        if (value.MistralMaxTokens is < 64 or > 8192 || value.MistralTemperature is < 0 or > 1) throw new ArgumentException("Os parâmetros do modelo estão fora dos limites permitidos.");
        if (value.AssistantContextMonths is < 1 or > 60 || value.AssistantMaxTransactions is < 10 or > 1000 || value.AssistantHistoryMessages is < 0 or > 50 || value.JournalSuggestionHistoryMessages is < 0 or > 50) throw new ArgumentException("Os limites da aplicação estão fora dos intervalos permitidos.");
    }
    
    /// <summary>
    /// Gets a string value from the dictionary or returns the fallback value if not found.
    /// </summary>
    /// <param name="values">The dictionary containing settings values.</param>
    /// <param name="key">The key to look up.</param>
    /// <param name="fallback">The fallback value to return if the key is not found.</param>
    /// <returns>The value from the dictionary or the fallback value.</returns>
    private static string Get(IReadOnlyDictionary<string, string> values, string key, string fallback) => values.GetValueOrDefault(key, fallback);

    private static string UpgradeDefault(string value, string legacyDefault, string currentDefault) =>
        string.Equals(value.Trim(), legacyDefault, StringComparison.Ordinal) ? currentDefault : value;
    
    /// <summary>
    /// Gets an integer value from the dictionary or returns the fallback value if not found or parsing fails.
    /// </summary>
    /// <param name="values">The dictionary containing settings values.</param>
    /// <param name="key">The key to look up.</param>
    /// <param name="fallback">The fallback value to return if the key is not found or parsing fails.</param>
    /// <returns>The parsed integer value or the fallback value.</returns>
    private static int GetInt(IReadOnlyDictionary<string, string> values, string key, int fallback) => int.TryParse(values.GetValueOrDefault(key), CultureInfo.InvariantCulture, out var value) ? value : fallback;
    
    /// <summary>
    /// Gets a double value from the dictionary or returns the fallback value if not found or parsing fails.
    /// </summary>
    /// <param name="values">The dictionary containing settings values.</param>
    /// <param name="key">The key to look up.</param>
    /// <param name="fallback">The fallback value to return if the key is not found or parsing fails.</param>
    /// <returns>The parsed double value or the fallback value.</returns>
    private static double GetDouble(IReadOnlyDictionary<string, string> values, string key, double fallback) => double.TryParse(values.GetValueOrDefault(key), CultureInfo.InvariantCulture, out var value) ? value : fallback;

}
