namespace DenariusAI.Domain.Enums;

/// <summary>
/// Represents the reconciliation status of a financial transaction.
/// </summary>
public enum ReconciliationStatus
{
    /// <summary>
    /// The transaction has not been reconciled yet.
    /// </summary>
    Unreconciled = 1,
    
    /// <summary>
    /// The transaction has been reconciled.
    /// </summary>
    Reconciled = 2
}
