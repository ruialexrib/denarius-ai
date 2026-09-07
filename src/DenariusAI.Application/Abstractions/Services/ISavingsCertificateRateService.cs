using DenariusAI.Application.DTOs;

namespace DenariusAI.Application.Abstractions.Services;

/// <summary>Provides persisted official Savings Certificate reference-rate history.</summary>
public interface ISavingsCertificateRateService
{
    /// <summary>Gets stored reference-rate observations for the requested recent period.</summary>
    /// <param name="months">Number of calendar months to include.</param>
    /// <param name="cancellationToken">Token used to cancel persistence access.</param>
    /// <returns>The filtered rate history and source metadata.</returns>
    Task<SavingsCertificateRateHistoryDto> GetHistoryAsync(int months, CancellationToken cancellationToken = default);

    /// <summary>Refreshes the recent official reference-rate history from the provider.</summary>
    /// <param name="actorId">Authenticated user identifier recorded on the persisted cache entry.</param>
    /// <param name="cancellationToken">Token used to cancel provider and persistence access.</param>
    /// <returns>The refresh outcome.</returns>
    Task<SavingsCertificateRateRefreshResultDto> RefreshAsync(string actorId, CancellationToken cancellationToken = default);
}
