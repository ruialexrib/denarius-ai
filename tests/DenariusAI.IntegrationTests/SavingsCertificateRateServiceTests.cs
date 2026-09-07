using System.Net;
using DenariusAI.Infrastructure.MarketData;
using DenariusAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DenariusAI.IntegrationTests;

/// <summary>Verifies deterministic IGCP rate parsing, persistence, filtering, and failure preservation.</summary>
public sealed class SavingsCertificateRateServiceTests
{
    /// <summary>Confirms a refresh persists twelve monthly observations and remains idempotent.</summary>
    /// <returns>A task completing after the assertions.</returns>
    [Fact]
    public async Task RefreshPersistsTwelveMonthsWithoutDuplicates()
    {
        await using var context = CreateContext();
        var handler = new IgcpHandler(valid: true);
        var service = new IgcpSavingsCertificateRateService(new HttpClient(handler), context);

        var first = await service.RefreshAsync("test-user");
        var second = await service.RefreshAsync("test-user");
        var history = await service.GetHistoryAsync(12);

        Assert.Equal(12, first.ImportedCount);
        Assert.Equal(12, first.StoredCount);
        Assert.Equal(12, second.StoredCount);
        Assert.Equal(12, history.Observations.Count);
        Assert.All(history.Observations, item => Assert.Equal(2.500m, item.GrossRate));
        Assert.Equal("IGCP", history.SourceName);
        Assert.NotNull(history.UpdatedAt);
        Assert.Single(context.ApplicationSettings.Where(item => item.Key == "SavingsCertificates.ReferenceRateHistory"));
        Assert.Contains(handler.RequestedUrls, url => url.Contains("-em-marco-de-", StringComparison.Ordinal));
    }

    /// <summary>Confirms period filtering returns only the requested recent calendar months.</summary>
    /// <returns>A task completing after the assertions.</returns>
    [Fact]
    public async Task HistoryFiltersToThreeSixAndTwelveMonths()
    {
        await using var context = CreateContext();
        var service = new IgcpSavingsCertificateRateService(new HttpClient(new IgcpHandler(valid: true)), context);
        await service.RefreshAsync("test-user");

        Assert.Equal(3, (await service.GetHistoryAsync(3)).Observations.Count);
        Assert.Equal(6, (await service.GetHistoryAsync(6)).Observations.Count);
        Assert.Equal(12, (await service.GetHistoryAsync(12)).Observations.Count);
    }

    /// <summary>Confirms an invalid provider refresh leaves previously persisted observations untouched.</summary>
    /// <returns>A task completing after the assertions.</returns>
    [Fact]
    public async Task InvalidRefreshPreservesExistingHistory()
    {
        await using var context = CreateContext();
        var validService = new IgcpSavingsCertificateRateService(new HttpClient(new IgcpHandler(valid: true)), context);
        await validService.RefreshAsync("test-user");
        var before = await validService.GetHistoryAsync(12);

        var invalidService = new IgcpSavingsCertificateRateService(new HttpClient(new IgcpHandler(valid: false)), context);
        await Assert.ThrowsAsync<InvalidOperationException>(() => invalidService.RefreshAsync("test-user"));
        var after = await validService.GetHistoryAsync(12);

        Assert.Equal(before.Observations, after.Observations);
    }

    /// <summary>Creates an isolated in-memory application database.</summary>
    /// <returns>The test database context.</returns>
    private static DenariusDbContext CreateContext() => new(new DbContextOptionsBuilder<DenariusDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    /// <summary>Returns deterministic IGCP-like HTML for every monthly request.</summary>
    /// <param name="valid">Whether the response contains a parseable official-rate sentence.</param>
    private sealed class IgcpHandler(bool valid) : HttpMessageHandler
    {
        /// <summary>Gets the requested IGCP publication URLs for URL-format assertions.</summary>
        public List<string> RequestedUrls { get; } = [];

        /// <summary>Returns the configured deterministic provider response.</summary>
        /// <param name="request">Outgoing provider request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>An HTTP response containing representative IGCP HTML.</returns>
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestedUrls.Add(request.RequestUri?.AbsoluteUri ?? string.Empty);
            var body = valid
                ? "<html><body>A taxa de juro bruta para novas subscrições de Certificados de Aforro, Série F, em setembro de 2026 foi fixada em 2,500%.</body></html>"
                : "<html><body>Conteúdo inesperado.</body></html>";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) });
        }
    }
}
