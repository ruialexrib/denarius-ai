using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Application.DTOs;
using DenariusAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DenariusAI.Infrastructure.MarketData;

/// <summary>Retrieves Série F reference rates from monthly IGCP publications and persists a local history cache.</summary>
/// <param name="httpClient">HTTP client used to access public IGCP pages.</param>
/// <param name="dbContext">Application database used to persist imported history and provider settings.</param>
public sealed class IgcpSavingsCertificateRateService(HttpClient httpClient, DenariusDbContext dbContext) : ISavingsCertificateRateService
{
    private const string StorageKey = "SavingsCertificates.ReferenceRateHistory";
    private const string SourceUrlKey = "SavingsCertificates.IgcpSourceUrl";
    private const string PublicationUrlTemplateKey = "SavingsCertificates.IgcpPublicationUrlTemplate";
    private const string SourceName = "IGCP";
    private const string DefaultSourceUrl = "https://www.igcp.pt/pt/aforristas/produtos-de-aforro/certificados-de-aforro";
    private const string DefaultPublicationUrlTemplate = "https://www.igcp.pt/pt/noticias/taxas-de-juro-dos-certificados-de-aforro-das-series-b-d-e-e-f-em-{month}-de-{year}";
    private const string Series = "F";
    private static readonly string[] MonthNames = ["janeiro", "fevereiro", "marco", "abril", "maio", "junho", "julho", "agosto", "setembro", "outubro", "novembro", "dezembro"];
    private static readonly Regex RateRegex = new(@"taxa\s+de\s+juro\s+bruta\s+para\s+novas\s+subscrições\s+de\s+Certificados\s+de\s+Aforro,\s*Série\s+F,.*?foi\s+fixada\s+em\s+(?<rate>\d{1,2}[,.]\d{1,5})%", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant);

    /// <inheritdoc />
    public async Task<SavingsCertificateRateHistoryDto> GetHistoryAsync(int months, CancellationToken cancellationToken = default)
    {
        if (months is < 1 or > 24) throw new ArgumentOutOfRangeException(nameof(months));
        var state = await LoadStateAsync(cancellationToken);
        var settings = await LoadProviderSettingsAsync(cancellationToken);
        var currentMonth = FirstDayOfMonth(DateOnly.FromDateTime(DateTime.Today));
        var from = currentMonth.AddMonths(-(months - 1));
        var observations = state.Observations
            .Where(item => item.Date >= from && item.Date <= currentMonth)
            .OrderBy(item => item.Date)
            .Select(item => new SavingsCertificateRateObservationDto(item.Date, item.Series, item.GrossRate))
            .ToArray();
        return new SavingsCertificateRateHistoryDto(observations, state.UpdatedAt, SourceName, settings.SourceUrl);
    }

    /// <inheritdoc />
    public async Task<SavingsCertificateRateRefreshResultDto> RefreshAsync(string actorId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(actorId)) throw new ArgumentException("An actor identifier is required.", nameof(actorId));
        var settings = await LoadProviderSettingsAsync(cancellationToken);
        var currentMonth = FirstDayOfMonth(DateOnly.FromDateTime(DateTime.Today));
        var imported = new List<StoredRateObservation>();
        for (var offset = 11; offset >= 0; offset--)
        {
            var month = currentMonth.AddMonths(-offset);
            var observation = await TryFetchMonthAsync(month, settings.PublicationUrlTemplate, cancellationToken);
            if (observation is not null) imported.Add(observation);
        }

        if (imported.Count == 0)
            throw new InvalidOperationException("O IGCP não devolveu taxas de Série F válidas para os últimos 12 meses.");

        var state = await LoadStateAsync(cancellationToken);
        var merged = state.Observations.ToDictionary(item => $"{item.Series}:{item.Date:yyyy-MM-dd}", StringComparer.OrdinalIgnoreCase);
        foreach (var item in imported) merged[$"{item.Series}:{item.Date:yyyy-MM-dd}"] = item;
        var updatedAt = DateTimeOffset.UtcNow;
        var newState = new StoredRateHistory(updatedAt, merged.Values.OrderBy(item => item.Date).ToArray());
        var serialized = JsonSerializer.Serialize(newState);
        var setting = await dbContext.ApplicationSettings.SingleOrDefaultAsync(item => item.Key == StorageKey, cancellationToken);
        if (setting is null)
        {
            dbContext.ApplicationSettings.Add(new ApplicationSetting { Key = StorageKey, Value = serialized, CreatedBy = actorId });
        }
        else
        {
            setting.Value = serialized;
            setting.UpdatedBy = actorId;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return new SavingsCertificateRateRefreshResultDto(imported.Count, newState.Observations.Count, updatedAt);
    }

    /// <summary>Loads the persisted rate-history cache, falling back to an empty state when it has not yet been created.</summary>
    /// <param name="cancellationToken">Token used to cancel database access.</param>
    /// <returns>The stored rate-history state.</returns>
    private async Task<StoredRateHistory> LoadStateAsync(CancellationToken cancellationToken)
    {
        var value = await dbContext.ApplicationSettings.AsNoTracking()
            .Where(item => item.Key == StorageKey)
            .Select(item => item.Value)
            .SingleOrDefaultAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(value)) return new StoredRateHistory(null, []);
        try
        {
            return JsonSerializer.Deserialize<StoredRateHistory>(value) ?? new StoredRateHistory(null, []);
        }
        catch (JsonException)
        {
            return new StoredRateHistory(null, []);
        }
    }

    /// <summary>Loads administrator-configured IGCP endpoints with safe defaults for existing installations.</summary>
    /// <param name="cancellationToken">Token used to cancel database access.</param>
    /// <returns>The effective provider URL settings.</returns>
    private async Task<ProviderSettings> LoadProviderSettingsAsync(CancellationToken cancellationToken)
    {
        var values = await dbContext.ApplicationSettings.AsNoTracking()
            .Where(item => item.Key == SourceUrlKey || item.Key == PublicationUrlTemplateKey)
            .ToDictionaryAsync(item => item.Key, item => item.Value, cancellationToken);
        return new ProviderSettings(
            values.GetValueOrDefault(SourceUrlKey, DefaultSourceUrl),
            values.GetValueOrDefault(PublicationUrlTemplateKey, DefaultPublicationUrlTemplate));
    }

    /// <summary>Retrieves and parses one monthly Série F publication.</summary>
    /// <param name="month">Month whose new-subscription rate should be loaded.</param>
    /// <param name="publicationUrlTemplate">Configured monthly publication URL template.</param>
    /// <param name="cancellationToken">Token used to cancel the HTTP request.</param>
    /// <returns>The parsed observation, or null when the monthly publication is unavailable or invalid.</returns>
    private async Task<StoredRateObservation?> TryFetchMonthAsync(DateOnly month, string publicationUrlTemplate, CancellationToken cancellationToken)
    {
        var monthName = MonthNames[month.Month - 1];
        var url = publicationUrlTemplate
            .Replace("{month}", Uri.EscapeDataString(monthName), StringComparison.Ordinal)
            .Replace("{year}", month.Year.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);
        using var response = await httpClient.GetAsync(url, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync(cancellationToken);
        var text = NormalizeHtml(html);
        var match = RateRegex.Match(text);
        if (!match.Success) return null;
        var normalizedRate = match.Groups["rate"].Value.Replace(',', '.');
        if (!decimal.TryParse(normalizedRate, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var rate) || rate is < 0m or > 100m) return null;
        return new StoredRateObservation(month, Series, rate);
    }

    /// <summary>Converts provider HTML into normalized plain text suitable for deterministic parsing.</summary>
    /// <param name="html">Raw provider HTML.</param>
    /// <returns>Decoded text with collapsed whitespace.</returns>
    private static string NormalizeHtml(string html)
    {
        var withoutTags = Regex.Replace(html, "<[^>]+>", " ", RegexOptions.CultureInvariant);
        return Regex.Replace(WebUtility.HtmlDecode(withoutTags), @"\s+", " ", RegexOptions.CultureInvariant).Trim();
    }

    /// <summary>Returns the first day of the supplied date's calendar month.</summary>
    /// <param name="date">Date to normalize.</param>
    /// <returns>The first day of the same month.</returns>
    private static DateOnly FirstDayOfMonth(DateOnly date) => new(date.Year, date.Month, 1);

    /// <summary>Represents the effective administrator-configured IGCP endpoints.</summary>
    /// <param name="SourceUrl">Official IGCP Savings Certificates source page.</param>
    /// <param name="PublicationUrlTemplate">Monthly publication URL template.</param>
    private sealed record ProviderSettings(string SourceUrl, string PublicationUrlTemplate);

    /// <summary>Represents the serialized local cache stored in application settings.</summary>
    /// <param name="UpdatedAt">Timestamp of the latest successful refresh.</param>
    /// <param name="Observations">Distinct imported observations.</param>
    private sealed record StoredRateHistory(DateTimeOffset? UpdatedAt, IReadOnlyList<StoredRateObservation> Observations);

    /// <summary>Represents one serialized provider observation.</summary>
    /// <param name="Date">Month to which the rate applies.</param>
    /// <param name="Series">Savings Certificate series.</param>
    /// <param name="GrossRate">Published gross annual percentage.</param>
    private sealed record StoredRateObservation(DateOnly Date, string Series, decimal GrossRate);
}
