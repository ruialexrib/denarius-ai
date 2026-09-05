using System.ComponentModel.DataAnnotations;
using DenariusAI.Application.DTOs;

namespace DenariusAI.Web.ViewModels;

/// <summary>Contains administrator-editable settings and AI availability for the settings form.</summary>
public sealed class ApplicationSettingsViewModel
{
    /// <summary>Gets or sets the ai provider setting.</summary>
    [Required, Display(Name = "Fornecedor de IA")] public string AiProvider { get; set; } = "Mistral";
    /// <summary>Gets or sets the mistral model setting.</summary>
    [Required, StringLength(100)] public string MistralModel { get; set; } = string.Empty;
    /// <summary>Gets or sets the mistral base url setting.</summary>
    [Required, Url, StringLength(300)] public string MistralBaseUrl { get; set; } = string.Empty;
    /// <summary>Gets or sets the ollama model setting.</summary>
    [Required, StringLength(100), Display(Name = "Modelo Ollama")] public string OllamaModel { get; set; } = "llama3.2";
    /// <summary>Gets or sets the ollama base url setting.</summary>
    [Required, Url, StringLength(300), Display(Name = "Servidor Ollama")] public string OllamaBaseUrl { get; set; } = "http://localhost:11434";
    /// <summary>Gets or sets the GroqCloud model identifier.</summary>
    [Required, StringLength(100)] public string GroqCloudModel { get; set; } = DenariusAI.Application.Configuration.GroqCloudDefaults.Model;
    /// <summary>Gets or sets the GroqCloud HTTPS API root.</summary>
    [Required, Url, StringLength(300)] public string GroqCloudBaseUrl { get; set; } = DenariusAI.Application.Configuration.GroqCloudDefaults.BaseUrl;
    /// <summary>Gets or sets the reasoning effort applied to GroqCloud GPT-OSS models.</summary>
    [Required, RegularExpression("low|medium|high")] public string GroqCloudReasoningEffort { get; set; } = DenariusAI.Application.Configuration.GroqCloudDefaults.ReasoningEffort;
    /// <summary>Gets or sets the serialized message budget for conversational AI workflows.</summary>
    [Range(4000, 64000)] public int AiMaxInputBytes { get; set; } = 12000;
    /// <summary>Gets or sets instructions for interpreting partial financial context.</summary>
    [Required, StringLength(10000)] public string AiContextGuidancePrompt { get; set; } = string.Empty;
    /// <summary>Gets or sets the maximum output tokens for any provider.</summary>
    [Range(64, 8192)] public int AiMaxTokens { get; set; }
    /// <summary>Gets or sets the generation temperature for any provider.</summary>
    [Range(0, 1)] public double AiTemperature { get; set; }
    /// <summary>Gets or sets the assistant system prompt setting.</summary>
    [Required, StringLength(10000)] public string AssistantSystemPrompt { get; set; } = string.Empty;
    /// <summary>Gets or sets the assistant context months setting.</summary>
    [Range(1, 60)] public int AssistantContextMonths { get; set; }
    /// <summary>Gets or sets the assistant max transactions setting.</summary>
    [Range(10, 1000)] public int AssistantMaxTransactions { get; set; }
    /// <summary>Gets or sets the assistant history messages setting.</summary>
    [Range(0, 50)] public int AssistantHistoryMessages { get; set; }
    /// <summary>Gets or sets the journal suggestion system prompt setting.</summary>
    [Required, StringLength(10000)] public string JournalSuggestionSystemPrompt { get; set; } = string.Empty;
    /// <summary>Gets or sets the journal suggestion history messages setting.</summary>
    [Range(0, 50)] public int JournalSuggestionHistoryMessages { get; set; }
    /// <summary>Gets or sets the reconciliation extraction prompt setting.</summary>
    [Required, StringLength(10000)] public string ReconciliationExtractionPrompt { get; set; } = string.Empty;
    /// <summary>Gets or sets the reconciliation classification prompt setting.</summary>
    [Required, StringLength(10000)] public string ReconciliationClassificationPrompt { get; set; } = string.Empty;
    /// <summary>Gets or sets the dashboard welcome prompt setting.</summary>
    [Required, StringLength(10000), Display(Name = "Prompt da mensagem de boas-vindas")] public string DashboardWelcomePrompt { get; set; } = string.Empty;
    /// <summary>Gets or sets the financial analysis prompt setting.</summary>
    [Required, StringLength(10000), Display(Name = "Prompt da análise financeira")] public string FinancialAnalysisPrompt { get; set; } = string.Empty;
    /// <summary>Gets or sets the connection test prompt setting.</summary>
    [Required, StringLength(1000), Display(Name = "Prompt do teste de ligação")] public string ConnectionTestPrompt { get; set; } = string.Empty;
    /// <summary>Gets or sets the correspondence metadata prompt setting.</summary>
    [Required, StringLength(10000), Display(Name = "Prompt dos metadados da correspondência")] public string CorrespondenceMetadataPrompt { get; set; } = string.Empty;
    /// <summary>Gets or sets the insurance clipboard prompt setting.</summary>
    [Required, StringLength(10000), Display(Name = "Prompt do preenchimento de seguros")] public string InsuranceClipboardPrompt { get; set; } = string.Empty;
    /// <summary>Gets or sets the savings certificate clipboard prompt setting.</summary>
    [Required, StringLength(10000), Display(Name = "Prompt do preenchimento de Certificados de Aforro")] public string SavingsCertificateClipboardPrompt { get; set; } = string.Empty;
    /// <summary>Gets or sets whether the selected provider is available.</summary>
    public bool AiIsConfigured { get; set; }
    /// <summary>Gets or sets the market data provider setting.</summary>
    [Required, StringLength(40)] public string MarketDataProvider { get; set; } = "AlphaVantage";
    /// <summary>Gets or sets the market data base url setting.</summary>
    [Required, Url, StringLength(300)] public string MarketDataBaseUrl { get; set; } = "https://www.alphavantage.co/query";

    /// <summary>Maps validated form values to application settings.</summary>
    /// <returns>The settings to persist.</returns>
    public ApplicationSettingsDto ToDto() => new(MistralModel, MistralBaseUrl, AiMaxTokens, AiTemperature, AssistantSystemPrompt, AssistantContextMonths, AssistantMaxTransactions, AssistantHistoryMessages, JournalSuggestionSystemPrompt, JournalSuggestionHistoryMessages, ReconciliationExtractionPrompt, ReconciliationClassificationPrompt, DashboardWelcomePrompt, FinancialAnalysisPrompt, ConnectionTestPrompt, CorrespondenceMetadataPrompt, MarketDataProvider, MarketDataBaseUrl, InsuranceClipboardPrompt, SavingsCertificateClipboardPrompt, AiProvider, OllamaModel, OllamaBaseUrl, AiMaxInputBytes, AiContextGuidancePrompt, GroqCloudModel, GroqCloudBaseUrl, GroqCloudReasoningEffort);
    /// <summary>Builds the settings form from effective configuration.</summary>
    /// <param name="value">The effective settings.</param>
    /// <param name="configured">Whether the selected provider is ready.</param>
    /// <returns>The populated form.</returns>
    public static ApplicationSettingsViewModel From(ApplicationSettingsDto value, bool configured) => new() { GroqCloudModel = value.GroqCloudModel, GroqCloudBaseUrl = value.GroqCloudBaseUrl, GroqCloudReasoningEffort = value.GroqCloudReasoningEffort, AiMaxInputBytes = value.AiMaxInputBytes, AiContextGuidancePrompt = value.AiContextGuidancePrompt, AiProvider = value.AiProvider, MistralModel = value.MistralModel, MistralBaseUrl = value.MistralBaseUrl, OllamaModel = value.OllamaModel, OllamaBaseUrl = value.OllamaBaseUrl, AiMaxTokens = value.AiMaxTokens, AiTemperature = value.AiTemperature, AssistantSystemPrompt = value.AssistantSystemPrompt, AssistantContextMonths = value.AssistantContextMonths, AssistantMaxTransactions = value.AssistantMaxTransactions, AssistantHistoryMessages = value.AssistantHistoryMessages, JournalSuggestionSystemPrompt = value.JournalSuggestionSystemPrompt, JournalSuggestionHistoryMessages = value.JournalSuggestionHistoryMessages, ReconciliationExtractionPrompt = value.ReconciliationExtractionPrompt, ReconciliationClassificationPrompt = value.ReconciliationClassificationPrompt, DashboardWelcomePrompt = value.DashboardWelcomePrompt, FinancialAnalysisPrompt = value.FinancialAnalysisPrompt, ConnectionTestPrompt = value.ConnectionTestPrompt, CorrespondenceMetadataPrompt = value.CorrespondenceMetadataPrompt, MarketDataProvider = value.MarketDataProvider, MarketDataBaseUrl = value.MarketDataBaseUrl, InsuranceClipboardPrompt = value.InsuranceClipboardPrompt, SavingsCertificateClipboardPrompt = value.SavingsCertificateClipboardPrompt, AiIsConfigured = configured };
}
