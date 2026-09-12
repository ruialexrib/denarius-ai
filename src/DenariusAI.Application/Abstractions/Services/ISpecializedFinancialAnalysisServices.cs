using DenariusAI.Application.DTOs;

namespace DenariusAI.Application.Abstractions.Services;

/// <summary>Builds deterministic savings and liquidity analysis.</summary>
public interface ISavingsLiquidityAnalysisService
{
    /// <summary>Gets savings and liquidity analysis for a selected interval.</summary>
    /// <param name="from">Selected interval start.</param>
    /// <param name="to">Selected interval end.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>Calculated savings and liquidity analysis.</returns>
    Task<SavingsLiquidityAnalysisDto> GetAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
}

/// <summary>Builds deterministic financial-assets analysis.</summary>
public interface IInvestmentPortfolioAnalysisService
{
    /// <summary>Gets the current tracked financial-assets analysis.</summary>
    /// <param name="asOf">Reference date shown in the analysis.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>Calculated financial-assets analysis.</returns>
    Task<InvestmentPortfolioAnalysisDto> GetAsync(DateOnly asOf, CancellationToken cancellationToken = default);
}

/// <summary>Builds deterministic future financial-commitments analysis.</summary>
public interface IFinancialCommitmentsAnalysisService
{
    /// <summary>Gets known insurance commitments for the following twelve months.</summary>
    /// <param name="asOf">Reference date.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>Calculated commitments analysis.</returns>
    Task<FinancialCommitmentsAnalysisDto> GetAsync(DateOnly asOf, CancellationToken cancellationToken = default);
}
