using DenariusAI.Domain.Common;

namespace DenariusAI.Domain.Entities;

/// <summary>
/// Represents a savings certificate investment entity.
/// </summary>
public sealed class SavingsCertificate : AuditableEntity
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SavingsCertificate"/> class.
    /// </summary>
    private SavingsCertificate() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="SavingsCertificate"/> class with the specified parameters.
    /// </summary>
    /// <param name="investmentDate">The date when the investment was made.</param>
    /// <param name="seriesNumber">The series or identification number of the certificate.</param>
    /// <param name="description">The description of the savings certificate.</param>
    /// <param name="investmentValue">The initial investment value.</param>
    /// <param name="rate">The interest rate of the certificate.</param>
    /// <param name="currentValue">The current value of the certificate.</param>
    /// <param name="nextCapitalization">The date of the next capitalization.</param>
    public SavingsCertificate(DateOnly investmentDate, string seriesNumber, string description,
        decimal investmentValue, decimal rate, decimal currentValue, DateOnly nextCapitalization)
        => Update(investmentDate, seriesNumber, description, investmentValue, rate, currentValue, nextCapitalization);

    /// <summary>
    /// Gets the date when the investment was made.
    /// </summary>
    public DateOnly InvestmentDate { get; private set; }

    /// <summary>
    /// Gets the series or identification number of the certificate.
    /// </summary>
    public string SeriesNumber { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the description of the savings certificate.
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the initial investment value.
    /// </summary>
    public decimal InvestmentValue { get; private set; }

    /// <summary>
    /// Gets the interest rate of the certificate.
    /// </summary>
    public decimal Rate { get; private set; }

    /// <summary>
    /// Gets the current value of the certificate.
    /// </summary>
    public decimal CurrentValue { get; private set; }

    /// <summary>
    /// Gets the date of the next capitalization.
    /// </summary>
    public DateOnly NextCapitalization { get; private set; }

    /// <summary>
    /// Updates the savings certificate with the specified parameters.
    /// </summary>
    /// <param name="investmentDate">The date when the investment was made.</param>
    /// <param name="seriesNumber">The series or identification number of the certificate.</param>
    /// <param name="description">The description of the savings certificate.</param>
    /// <param name="investmentValue">The initial investment value.</param>
    /// <param name="rate">The interest rate of the certificate.</param>
    /// <param name="currentValue">The current value of the certificate.</param>
    /// <param name="nextCapitalization">The date of the next capitalization.</param>
    /// <exception cref="ArgumentException">Thrown when series number or description is null or whitespace.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when investment value is not positive, or current value or rate is negative.</exception>
    public void Update(DateOnly investmentDate, string seriesNumber, string description,
        decimal investmentValue, decimal rate, decimal currentValue, DateOnly nextCapitalization)
    {
        if (string.IsNullOrWhiteSpace(seriesNumber)) throw new ArgumentException("A série/número é obrigatória.");
        if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("A descrição é obrigatória.");
        if (investmentValue <= 0) throw new ArgumentOutOfRangeException(nameof(investmentValue), "O investimento deve ser superior a zero.");
        if (currentValue < 0) throw new ArgumentOutOfRangeException(nameof(currentValue), "O valor atual não pode ser negativo.");
        if (rate < 0) throw new ArgumentOutOfRangeException(nameof(rate), "A taxa não pode ser negativa.");

        InvestmentDate = investmentDate;
        SeriesNumber = seriesNumber.Trim();
        Description = description.Trim();
        InvestmentValue = investmentValue;
        Rate = rate;
        CurrentValue = currentValue;
        NextCapitalization = nextCapitalization;
    }
}
