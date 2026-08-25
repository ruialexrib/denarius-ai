namespace DenariusAI.Domain.Enums;

/// <summary>
/// Represents the type of account in the financial system.
/// </summary>
public enum AccountType
{
    /// <summary>
    /// Represents a bank account.
    /// </summary>
    BankAccount = 1,
    
    /// <summary>
    /// Represents cash on hand.
    /// </summary>
    Cash = 2,
    
    /// <summary>
    /// Represents a savings account.
    /// </summary>
    Savings = 3,
    
    /// <summary>
    /// Represents a term deposit account.
    /// </summary>
    TermDeposit = 4,
    
    /// <summary>
    /// Represents an investment account.
    /// </summary>
    Investment = 5,
    
    /// <summary>
    /// Represents other types of assets.
    /// </summary>
    OtherAsset = 6,
    
    /// <summary>
    /// Represents an income account.
    /// </summary>
    Income = 7,
    
    /// <summary>
    /// Represents an expense account.
    /// </summary>
    Expense = 8
}
