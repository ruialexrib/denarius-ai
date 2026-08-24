using DenariusAI.Domain.Common;

namespace DenariusAI.Domain.Entities;

public sealed class SavingsCertificate : AuditableEntity
{
    private SavingsCertificate() { }

    public SavingsCertificate(DateOnly investmentDate, string seriesNumber, string description,
        decimal investmentValue, decimal rate, decimal currentValue, DateOnly nextCapitalization)
        => Update(investmentDate, seriesNumber, description, investmentValue, rate, currentValue, nextCapitalization);

    public DateOnly InvestmentDate { get; private set; }
    public string SeriesNumber { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal InvestmentValue { get; private set; }
    public decimal Rate { get; private set; }
    public decimal CurrentValue { get; private set; }
    public DateOnly NextCapitalization { get; private set; }

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
