using DenariusAI.Application.Abstractions.Persistence;
using DenariusAI.Application.Abstractions.Services;
using DenariusAI.Application.DTOs;
using DenariusAI.Domain.Enums;

namespace DenariusAI.Application.Services;

/// <summary>Calculates known insurance-premium commitments, overdue amounts and renewal pressure.</summary>
/// <param name="repository">Read-only source for insurance policies and premiums.</param>
public sealed class FinancialCommitmentsAnalysisService(
    ISpecializedFinancialAnalysisRepository repository) : IFinancialCommitmentsAnalysisService
{
    /// <inheritdoc />
    public async Task<FinancialCommitmentsAnalysisDto> GetAsync(
        DateOnly asOf,
        CancellationToken cancellationToken = default)
    {
        var horizonEnd = asOf.AddYears(1).AddDays(-1);
        var active = (await repository.GetInsurancePoliciesAsync(cancellationToken))
            .Where(policy => policy.Status == InsurancePolicyStatus.Active)
            .ToList();

        var policyRows = active.Select(policy =>
        {
            var horizonPremiums = policy.Premiums
                .Where(premium => premium.DueDate >= asOf && premium.DueDate <= horizonEnd)
                .ToList();
            var outstanding = policy.Premiums
                .Where(premium => premium.DueDate <= asOf && !premium.IsPaid)
                .ToList();
            return new CommitmentPolicyAnalysisDto(
                policy.Id,
                policy.Name,
                policy.Insurer,
                TypeLabel(policy.Type),
                FrequencyLabel(policy.PaymentFrequency),
                policy.RenewalDate,
                horizonPremiums.Sum(premium => premium.Amount),
                outstanding.Sum(premium => premium.Amount),
                outstanding.Count,
                horizonPremiums.Where(premium => !premium.IsPaid).Select(premium => (DateOnly?)premium.DueDate).Min());
        }).OrderByDescending(item => item.OutstandingAmount > 0m)
            .ThenBy(item => item.NextDueDate)
            .ThenBy(item => item.Name)
            .ToList();

        var futurePremiums = active.SelectMany(policy => policy.Premiums)
            .Where(premium => premium.DueDate >= asOf && premium.DueDate <= horizonEnd)
            .ToList();
        var overdue = active.SelectMany(policy => policy.Premiums)
            .Where(premium => premium.DueDate <= asOf && !premium.IsPaid)
            .ToList();

        var calendar = futurePremiums
            .GroupBy(premium => new { premium.DueDate.Year, premium.DueDate.Month })
            .Select(group => new CommitmentMonthAnalysisDto(
                group.Key.Year,
                group.Key.Month,
                group.Sum(item => item.Amount),
                group.Where(item => !item.IsPaid && item.DueDate <= asOf).Sum(item => item.Amount),
                group.Count()))
            .OrderBy(item => item.Year)
            .ThenBy(item => item.Month)
            .ToList();

        var totalScheduled = futurePremiums.Sum(item => item.Amount);
        var typeRows = active.GroupBy(policy => policy.Type)
            .Select(group =>
            {
                var amount = group.SelectMany(policy => policy.Premiums)
                    .Where(premium => premium.DueDate >= asOf && premium.DueDate <= horizonEnd)
                    .Sum(premium => premium.Amount);
                return new CommitmentTypeAnalysisDto(
                    TypeLabel(group.Key),
                    amount,
                    totalScheduled == 0m ? 0m : decimal.Round(amount / totalScheduled * 100m, 1),
                    group.Count());
            })
            .OrderByDescending(item => item.ScheduledAmount)
            .ThenBy(item => item.Type)
            .ToList();

        var renewals = active.Count(policy => policy.RenewalDate is { } renewal
            && renewal >= asOf
            && renewal <= asOf.AddDays(90));
        var findings = BuildFindings(policyRows, calendar, typeRows, overdue.Sum(item => item.Amount), renewals, totalScheduled);

        return new(
            asOf,
            horizonEnd,
            active.Count,
            totalScheduled,
            overdue.Sum(item => item.Amount),
            overdue.Count,
            renewals,
            futurePremiums.Where(item => item.IsPaid).Sum(item => item.Amount),
            policyRows,
            calendar,
            typeRows,
            findings);
    }

    /// <summary>Builds prioritised deterministic findings for known insurance commitments.</summary>
    /// <param name="policies">Analysed active policies.</param>
    /// <param name="calendar">Monthly known premium schedule.</param>
    /// <param name="types">Commitment concentration by insurance type.</param>
    /// <param name="outstandingAmount">Overdue unpaid amount.</param>
    /// <param name="renewals">Renewals within the next 90 days.</param>
    /// <param name="scheduled">Known premiums in the twelve-month horizon.</param>
    /// <returns>Prioritised findings.</returns>
    private static IReadOnlyList<SpecializedAnalysisFindingDto> BuildFindings(
        IReadOnlyList<CommitmentPolicyAnalysisDto> policies,
        IReadOnlyList<CommitmentMonthAnalysisDto> calendar,
        IReadOnlyList<CommitmentTypeAnalysisDto> types,
        decimal outstandingAmount,
        int renewals,
        decimal scheduled)
    {
        var findings = new List<SpecializedAnalysisFindingDto>();
        if (outstandingAmount > 0m)
        {
            findings.Add(new(
                "negative",
                "Prémios vencidos por regularizar",
                $"Existem {outstandingAmount:N2} € de prémios vencidos sem pagamento associado.",
                "Insurance",
                "Index"));
        }

        if (renewals > 0)
        {
            findings.Add(new(
                "warning",
                "Renovações próximas",
                $"{renewals} apólice(s) ativa(s) têm renovação prevista nos próximos 90 dias.",
                "Insurance",
                "Index"));
        }

        var peakMonth = calendar.OrderByDescending(item => item.ScheduledAmount).FirstOrDefault();
        if (peakMonth is not null && peakMonth.ScheduledAmount > 0m)
        {
            findings.Add(new(
                "info",
                "Mês com maior pressão conhecida",
                $"{peakMonth.Month:D2}/{peakMonth.Year} concentra {peakMonth.ScheduledAmount:N2} € de prémios conhecidos."));
        }

        var topType = types.FirstOrDefault();
        if (topType is not null && topType.Weight >= 50m && scheduled > 0m)
        {
            findings.Add(new(
                "warning",
                "Encargos concentrados num tipo de seguro",
                $"{topType.Type} representa {topType.Weight:N1}% dos prémios conhecidos nos próximos 12 meses."));
        }

        var largestPolicy = policies.OrderByDescending(item => item.ScheduledAmount).FirstOrDefault();
        if (largestPolicy is not null && largestPolicy.ScheduledAmount > 0m)
        {
            findings.Add(new(
                "info",
                "Maior compromisso por apólice",
                $"{largestPolicy.Name} representa {largestPolicy.ScheduledAmount:N2} € de prémios conhecidos no horizonte.",
                "Insurance",
                "Details",
                largestPolicy.Id));
        }

        if (scheduled == 0m && policies.Count > 0)
        {
            findings.Add(new(
                "info",
                "Sem prémios futuros registados",
                "Existem apólices ativas, mas não há prémios com vencimento registado nos próximos 12 meses. A análise não inventa periodicidades em falta."));
        }

        return findings.Take(6).ToList();
    }

    /// <summary>Gets the European Portuguese insurance-type label.</summary>
    /// <param name="type">Insurance type.</param>
    /// <returns>User-facing label.</returns>
    private static string TypeLabel(InsurancePolicyType type) => type switch
    {
        InsurancePolicyType.Home => "Habitação",
        InsurancePolicyType.Motor => "Automóvel",
        InsurancePolicyType.Health => "Saúde",
        InsurancePolicyType.Life => "Vida",
        InsurancePolicyType.PersonalAccident => "Acidentes pessoais",
        _ => "Outro"
    };

    /// <summary>Gets the European Portuguese payment-frequency label.</summary>
    /// <param name="frequency">Payment frequency.</param>
    /// <returns>User-facing label.</returns>
    private static string FrequencyLabel(InsurancePaymentFrequency frequency) => frequency switch
    {
        InsurancePaymentFrequency.Monthly => "Mensal",
        InsurancePaymentFrequency.Quarterly => "Trimestral",
        InsurancePaymentFrequency.Semiannual => "Semestral",
        InsurancePaymentFrequency.Annual => "Anual",
        _ => "Irregular"
    };
}
