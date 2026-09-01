using DenariusAI.Domain.Entities;
using DenariusAI.Domain.Enums;

namespace DenariusAI.UnitTests;

/// <summary>Tests insurance portfolio domain invariants.</summary>
public sealed class InsurancePortfolioTests
{
    /// <summary>Verifies invalid policy dates are rejected.</summary>
    [Fact]
    public void Policy_EndBeforeStart_Throws() =>
        Assert.Throws<ArgumentException>(() => new InsurancePolicy("Casa", "Seguradora", "P-1", InsurancePolicyType.Home, InsurancePaymentFrequency.Annual, new DateOnly(2026, 1, 1), new DateOnly(2025, 12, 31)));

    /// <summary>Verifies premiums must have a positive amount.</summary>
    [Fact]
    public void Premium_NonPositiveAmount_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new InsurancePremium(Guid.NewGuid(), 0m, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), new DateOnly(2026, 1, 1)));

    /// <summary>Verifies a premium can explicitly associate and disassociate a movement.</summary>
    [Fact]
    public void Premium_MovementAssociation_IsUserControlled()
    {
        var premium = new InsurancePremium(Guid.NewGuid(), 120m, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), new DateOnly(2026, 1, 10));
        var movementId = Guid.NewGuid();
        premium.AssociateMovement(movementId);
        Assert.Equal(movementId, premium.JournalEntryId);
        premium.RemoveMovementAssociation();
        Assert.Null(premium.JournalEntryId);
    }

    /// <summary>Verifies premium attachments only accept PDF content.</summary>
    [Fact]
    public void Attachment_NonPdf_Throws() =>
        Assert.Throws<ArgumentException>(() => new InsurancePremiumAttachment(Guid.NewGuid(), "receipt.txt", "text/plain", "dGVzdA=="));

    /// <summary>Verifies general policy attachments only accept PDF content.</summary>
    [Fact]
    public void PolicyAttachment_NonPdf_Throws() =>
        Assert.Throws<ArgumentException>(() => new InsurancePolicyAttachment(Guid.NewGuid(), "contract.txt", "text/plain", "dGVzdA=="));
}
