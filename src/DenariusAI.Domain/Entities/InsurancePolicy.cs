using DenariusAI.Domain.Common;
using DenariusAI.Domain.Enums;

namespace DenariusAI.Domain.Entities;

/// <summary>
/// Represents an insurance policy monitored by the household.
/// </summary>
public sealed class InsurancePolicy : AuditableEntity
{
    private readonly List<InsurancePremium> _premiums = [];
    private readonly List<InsurancePolicyAttachment> _attachments = [];

    /// <summary>Initializes an empty policy for Entity Framework Core.</summary>
    private InsurancePolicy() { }

    /// <summary>Creates an insurance policy.</summary>
    /// <param name="name">Policy display name.</param>
    /// <param name="insurer">Insurance provider.</param>
    /// <param name="policyNumber">Provider policy number.</param>
    /// <param name="type">Insurance type.</param>
    /// <param name="frequency">Usual payment frequency.</param>
    /// <param name="startDate">Coverage start date.</param>
    /// <param name="endDate">Optional coverage end date.</param>
    /// <param name="renewalDate">Optional next renewal date.</param>
    /// <param name="insuredSubject">Optional insured object or person description.</param>
    /// <param name="notes">Optional notes.</param>
    public InsurancePolicy(string name, string insurer, string policyNumber, InsurancePolicyType type,
        InsurancePaymentFrequency frequency, DateOnly startDate, DateOnly? endDate = null,
        DateOnly? renewalDate = null, string? insuredSubject = null, string? notes = null)
    {
        Update(name, insurer, policyNumber, type, frequency, startDate, endDate, renewalDate, insuredSubject, notes);
    }

    /// <summary>Gets the policy display name.</summary>
    public string Name { get; private set; } = string.Empty;
    /// <summary>Gets the insurer.</summary>
    public string Insurer { get; private set; } = string.Empty;
    /// <summary>Gets the policy number.</summary>
    public string PolicyNumber { get; private set; } = string.Empty;
    /// <summary>Gets the policy type.</summary>
    public InsurancePolicyType Type { get; private set; }
    /// <summary>Gets the normal premium frequency.</summary>
    public InsurancePaymentFrequency PaymentFrequency { get; private set; }
    /// <summary>Gets the coverage start date.</summary>
    public DateOnly StartDate { get; private set; }
    /// <summary>Gets the optional coverage end date.</summary>
    public DateOnly? EndDate { get; private set; }
    /// <summary>Gets the optional next renewal date.</summary>
    public DateOnly? RenewalDate { get; private set; }
    /// <summary>Gets the insured object or person description.</summary>
    public string? InsuredSubject { get; private set; }
    /// <summary>Gets optional policy notes.</summary>
    public string? Notes { get; private set; }
    /// <summary>Gets the policy lifecycle status.</summary>
    public InsurancePolicyStatus Status { get; private set; } = InsurancePolicyStatus.Active;
    /// <summary>Gets the premium history.</summary>
    public IReadOnlyCollection<InsurancePremium> Premiums => _premiums.AsReadOnly();
    /// <summary>Gets the general policy documents.</summary>
    public IReadOnlyCollection<InsurancePolicyAttachment> Attachments => _attachments.AsReadOnly();

    /// <summary>Updates the editable policy details.</summary>
    /// <param name="name">Policy display name.</param>
    /// <param name="insurer">Insurance provider.</param>
    /// <param name="policyNumber">Provider policy number.</param>
    /// <param name="type">Insurance type.</param>
    /// <param name="frequency">Usual payment frequency.</param>
    /// <param name="startDate">Coverage start date.</param>
    /// <param name="endDate">Optional coverage end date.</param>
    /// <param name="renewalDate">Optional next renewal date.</param>
    /// <param name="insuredSubject">Optional insured object or person description.</param>
    /// <param name="notes">Optional notes.</param>
    /// <exception cref="ArgumentException">Thrown for missing required values or invalid dates.</exception>
    public void Update(string name, string insurer, string policyNumber, InsurancePolicyType type,
        InsurancePaymentFrequency frequency, DateOnly startDate, DateOnly? endDate, DateOnly? renewalDate,
        string? insuredSubject, string? notes)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Policy name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(insurer)) throw new ArgumentException("Insurer is required.", nameof(insurer));
        if (string.IsNullOrWhiteSpace(policyNumber)) throw new ArgumentException("Policy number is required.", nameof(policyNumber));
        if (endDate.HasValue && endDate.Value < startDate) throw new ArgumentException("Policy end date cannot precede its start date.", nameof(endDate));
        if (renewalDate.HasValue && renewalDate.Value < startDate) throw new ArgumentException("Renewal date cannot precede the policy start date.", nameof(renewalDate));
        Name = name.Trim(); Insurer = insurer.Trim(); PolicyNumber = policyNumber.Trim(); Type = type; PaymentFrequency = frequency;
        StartDate = startDate; EndDate = endDate; RenewalDate = renewalDate; InsuredSubject = Normalize(insuredSubject); Notes = Normalize(notes);
    }

    /// <summary>Archives the policy while preserving its history.</summary>
    public void Archive() => Status = InsurancePolicyStatus.Archived;

    /// <summary>Cancels the policy while preserving its history.</summary>
    public void Cancel() => Status = InsurancePolicyStatus.Cancelled;

    /// <summary>Reactivates a previously archived or cancelled policy.</summary>
    public void Activate() => Status = InsurancePolicyStatus.Active;

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
