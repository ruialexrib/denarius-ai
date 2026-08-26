using DenariusAI.Domain.Entities;
using DenariusAI.Domain.Enums;

namespace DenariusAI.UnitTests;

/// <summary>
/// Contains definitions for JournalEntryTests.
/// </summary>
public sealed class JournalEntryTests
{
    private static readonly Guid BankAccountId = Guid.Parse("30000000-0000-0000-0000-000000000001");
    private static readonly Guid ExpenseAccountId = Guid.Parse("30000000-0000-0000-0000-000000000002");

    [Fact]
    public void BalancedEntryIsAcceptedAndCalculatesTotals()
    {
        var entry = CreateEntry();
        entry.AddLine(ExpenseAccountId, 35m, 0m, "Água");
        entry.AddLine(BankAccountId, 0m, 35m, "Conta principal");

        entry.EnsureBalanced();

        Assert.Equal(35m, entry.TotalDebit);
        Assert.Equal(35m, entry.TotalCredit);
        Assert.Equal(0m, entry.Difference);
    }

    [Fact]
    public void UnbalancedEntryIsRejected()
    {
        var entry = CreateEntry();
        entry.AddLine(ExpenseAccountId, 35m, 0m);
        entry.AddLine(BankAccountId, 0m, 30m);

        var exception = Assert.Throws<InvalidOperationException>(entry.EnsureBalanced);
        Assert.Equal("Total debit must equal total credit.", exception.Message);
    }

    [Fact]
    public void EntryWithLessThanTwoLinesIsRejected()
    {
        var entry = CreateEntry();
        entry.AddLine(ExpenseAccountId, 35m, 0m);

        Assert.Throws<InvalidOperationException>(entry.EnsureBalanced);
    }

    [Theory]
    [InlineData(10, 10)]
    [InlineData(0, 0)]
    [InlineData(-1, 0)]
    public void InvalidDebitAndCreditCombinationIsRejected(decimal debit, decimal credit)
    {
        var entry = CreateEntry();
        Assert.ThrowsAny<ArgumentException>(() => entry.AddLine(BankAccountId, debit, credit));
    }

    [Fact]
    public void CancelledEntryPreservesLinesAndCannotBeChanged()
    {
        var entry = CreateEntry();
        entry.AddLine(ExpenseAccountId, 35m, 0m);
        entry.AddLine(BankAccountId, 0m, 35m);

        entry.Cancel("user-id", DateTimeOffset.UtcNow);

        Assert.Equal(JournalEntryStatus.Cancelled, entry.Status);
        Assert.Equal(2, entry.Lines.Count);
        Assert.Throws<InvalidOperationException>(() => entry.AddLine(BankAccountId, 1m, 0m));
    }

    private static JournalEntry CreateEntry() => new(new DateOnly(2026, 8, 24), "Pagamento de água");
}
