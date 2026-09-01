using DenariusAI.Domain.Entities;
using DenariusAI.Domain.Enums;
using DenariusAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DenariusAI.IntegrationTests;

/// <summary>Verifies persistence of insurance policies and their general documentation.</summary>
public sealed class InsurancePersistenceTests
{
    /// <summary>Verifies a general policy document survives a database round trip without requiring a premium.</summary>
    [Fact]
    public async Task PolicyAttachmentCanBePersistedWithoutPremium()
    {
        await using var context = new DenariusDbContext(new DbContextOptionsBuilder<DenariusDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var policy = new InsurancePolicy("Automóvel", "Seguradora", "AUTO-1", InsurancePolicyType.Motor,
            InsurancePaymentFrequency.Annual, new DateOnly(2026, 1, 1));
        var attachment = new InsurancePolicyAttachment(policy.Id, "condicoes.pdf", "application/pdf", "JVBERi0xLjQ=");
        context.AddRange(policy, attachment);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var stored = await context.InsurancePolicies.Include(item => item.Attachments).SingleAsync();

        Assert.Empty(stored.Premiums);
        var document = Assert.Single(stored.Attachments);
        Assert.Equal("condicoes.pdf", document.FileName);
        Assert.Equal(policy.Id, document.PolicyId);
    }
}
