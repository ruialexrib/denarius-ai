using DenariusAI.Application.DTOs;

namespace DenariusAI.Application.Abstractions.Persistence;

/// <summary>Provides read-only persisted facts required by specialised financial analyses.</summary>
public interface ISpecializedFinancialAnalysisRepository
{
    /// <summary>Gets owned and watchlist stock facts with the first available historical price.</summary>
    /// <param name="cancellationToken">Token used to cancel persistence access.</param>
    /// <returns>Tracked stock facts.</returns>
    Task<IReadOnlyList<InvestmentStockFactDto>> GetStocksAsync(CancellationToken cancellationToken = default);

    /// <summary>Gets insurance policies with their known premium schedule.</summary>
    /// <param name="cancellationToken">Token used to cancel persistence access.</param>
    /// <returns>Insurance policy facts.</returns>
    Task<IReadOnlyList<InsurancePolicyFactDto>> GetInsurancePoliciesAsync(CancellationToken cancellationToken = default);
}
